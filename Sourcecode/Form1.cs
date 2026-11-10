using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace BC_Progress
{
    public partial class Form1 : Form
    {
        private const int WsExComposited = 0x02000000;

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= WsExComposited;
                return createParams;
            }
        }

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly ProgressDocumentService _progressDocumentService;

        private ProgressDocument _progressDocument = new();
        private string _progressFilePath = string.Empty;
        private readonly BindingList<CampaignProgressRow> _campaignRows = [];
        private readonly string _imageCacheDirectory;
        private static readonly HttpClient _httpClient = new();
        private static readonly SemaphoreSlim _imageLoadSemaphore = new(4, 4);
        private static readonly SemaphoreSlim _evolutionLoadSemaphore = new(4, 4);
        private readonly object _evolutionCacheLock = new();
        private readonly Dictionary<string, EvolutionInfo?> _evolutionInfoCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Task<EvolutionInfo?>> _evolutionInfoTasks = new(StringComparer.OrdinalIgnoreCase);
        private bool _isGridLocked = true;
        private Panel? _draggedTile;

        public Form1()
        {
            InitializeComponent();
            _progressDocumentService = new ProgressDocumentService(_jsonOptions);
            _imageCacheDirectory = Path.Combine(AppContext.BaseDirectory, "cache", "cat-icons");
            Directory.CreateDirectory(_imageCacheDirectory);
            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BCProgress", "1.0"));
            }
            LoadProgressDocument();
            EnsureCatViewOrderInitialized();
            ConfigureGridLockToggle();
            ConfigureCatDragAndDrop();
            ConfigureCampaignGrid();
            RenderCatTiles();
            BindCampaignProgress();
        }

        private void ConfigureGridLockToggle()
        {
            _isGridLocked = toggleGridLockToolStripMenuItem.Checked;
            toggleGridLockToolStripMenuItem.CheckedChanged += (_, _) =>
            {
                _isGridLocked = toggleGridLockToolStripMenuItem.Checked;
            };

            refreshImagesToolStripMenuItem.Click += async (_, _) => await RefreshAllCatImagesAsync();
            importProgressJsonToolStripMenuItem.Click += ImportProgressJsonToolStripMenuItem_Click;
            exportToolStripMenuItem.Click += ExportToolStripMenuItem_Click;
        }

        private void ImportProgressJsonToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Import Progress.json",
                FileName = "Progress.json"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _progressDocument = _progressDocumentService.ImportFromFile(dialog.FileName);
                EnsureCatViewOrderInitialized();
                SaveProgressDocument();
                RenderCatTiles();
                BindCampaignProgress();

                MessageBox.Show(this, "Progress imported successfully.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Import failed:\n{ex.Message}", "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Export Progress",
                FileName = "Progress.json",
                DefaultExt = "json",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                _progressDocumentService.ExportToFile(dialog.FileName, _progressDocument);

                MessageBox.Show(this, "Progress exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureCatDragAndDrop()
        {
            flowCats.AllowDrop = true;
            flowCats.DragEnter += FlowCats_DragEnter;
            flowCats.DragOver += FlowCats_DragOver;
            flowCats.DragDrop += FlowCats_DragDrop;
        }

        private void FlowCats_DragEnter(object? sender, DragEventArgs e)
        {
            if (!_isGridLocked && e.Data?.GetDataPresent(typeof(Panel)) == true)
            {
                e.Effect = DragDropEffects.Move;
                return;
            }

            e.Effect = DragDropEffects.None;
        }

        private void FlowCats_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = !_isGridLocked && e.Data?.GetDataPresent(typeof(Panel)) == true
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void FlowCats_DragDrop(object? sender, DragEventArgs e)
        {
            if (_isGridLocked || _draggedTile is null)
            {
                return;
            }

            if (_draggedTile.Tag is not CatTileBinding draggedBinding)
            {
                _draggedTile = null;
                return;
            }

            var dropPoint = flowCats.PointToClient(new Point(e.X, e.Y));
            var targetControl = flowCats.GetChildAtPoint(dropPoint);
            var targetTile = FindTilePanel(targetControl);

            if (targetTile is null)
            {
                _draggedTile = null;
                return;
            }

            if (targetTile.Tag is not CatTileBinding targetBinding)
            {
                _draggedTile = null;
                return;
            }

            if (!string.Equals(draggedBinding.Category, targetBinding.Category, StringComparison.OrdinalIgnoreCase))
            {
                _draggedTile = null;
                return;
            }

            if (!ReferenceEquals(targetTile, _draggedTile))
            {
                var targetIndex = flowCats.Controls.GetChildIndex(targetTile, false);
                flowCats.Controls.SetChildIndex(_draggedTile, targetIndex);
            }

            flowCats.Invalidate();
            UpdateViewOrderFromTiles();
            SaveProgressDocument();
            _draggedTile = null;
        }

        private void Tile_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_isGridLocked || e.Button != MouseButtons.Left)
            {
                return;
            }

            if (sender is Control control && IsInteractiveEditor(control))
            {
                return;
            }

            var tile = FindTilePanel(sender as Control);
            if (tile is null)
            {
                return;
            }

            _draggedTile = tile;
            tile.DoDragDrop(tile, DragDropEffects.Move);
        }

        private static Panel? FindTilePanel(Control? control)
        {
            while (control is not null)
            {
                if (control is Panel panel && panel.Tag is CatTileBinding)
                {
                    return panel;
                }

                control = control.Parent;
            }

            return null;
        }

        private static bool IsInteractiveEditor(Control control)
        {
            return control is Button || control is NumericUpDown;
        }

        private void EnsureCatViewOrderInitialized()
        {
            var changed = false;
            foreach (var category in _progressDocument.Categories.OrderBy(x => x.Key))
            {
                var ownedCats = category.Value.Where(cat => cat.Owned).ToList();
                var currentMax = ownedCats
                    .Where(x => x.ViewOrder.HasValue)
                    .Select(x => x.ViewOrder!.Value)
                    .DefaultIfEmpty(-1)
                    .Max();

                var next = currentMax + 1;
                foreach (var cat in ownedCats.Where(x => !x.ViewOrder.HasValue))
                {
                    cat.ViewOrder = next++;
                    changed = true;
                }
            }

            if (changed)
            {
                SaveProgressDocument();
            }
        }

        private void UpdateViewOrderFromTiles()
        {
            var categoryIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var tile in flowCats.Controls.OfType<Panel>())
            {
                if (tile.Tag is CatTileBinding binding)
                {
                    if (!categoryIndices.TryGetValue(binding.Category, out var index))
                    {
                        index = 0;
                    }

                    binding.Entry.ViewOrder = index;
                    categoryIndices[binding.Category] = index + 1;
                }
            }
        }

        private void RenderCatTiles()
        {
            flowCats.SuspendLayout();

            foreach (Control control in flowCats.Controls)
            {
                control.Dispose();
            }

            flowCats.Controls.Clear();

            var ownedCats = GetOwnedCatsForView().ToList();
            foreach (var item in ownedCats)
            {
                flowCats.Controls.Add(CreateCatTile(item.Category, item.Entry));
            }

            flowCats.ResumeLayout();

            StartEvolutionPreloadForVisibleCats(ownedCats);
        }

        private Panel CreateCatTile(string category, CatEntry entry)
        {
            var tile = new Panel
            {
                Width = 210,
                Height = 272,
                Margin = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = GetCategoryColor(category)
            };
            tile.Tag = new CatTileBinding(category, entry);
            tile.MouseDown += Tile_MouseDown;
            tile.MouseUp += Tile_MouseUp;

            var picture = new PictureBox
            {
                Width = 170,
                Height = 120,
                Left = 19,
                Top = 8,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Image = CreatePlaceholderImage(entry.Name)
            };
            picture.MouseDown += Tile_MouseDown;
            picture.MouseUp += Tile_MouseUp;

            var nameLabel = new Label
            {
                Left = 8,
                Top = 134,
                Width = 192,
                Height = 36,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font, FontStyle.Bold),
                Text = entry.Name
            };
            nameLabel.MouseDown += Tile_MouseDown;
            nameLabel.MouseUp += Tile_MouseUp;

            var rarityLabel = new Label
            {
                Left = 8,
                Top = 171,
                Width = 192,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = FormatCategoryName(category)
            };
            rarityLabel.MouseDown += Tile_MouseDown;
            rarityLabel.MouseUp += Tile_MouseUp;

            _ = LoadCatImageAsync(picture, category, entry.Name);

            AddLevelRow(tile, "Base", 193, entry.LevelBase ?? 0, value =>
            {
                entry.LevelBase = value;
                entry.LevelUnknown = false;
                SaveProgressDocument();
            });

            AddLevelRow(tile, "Plus", 228, entry.LevelPlus ?? 0, value =>
            {
                entry.LevelPlus = value;
                entry.LevelUnknown = false;
                SaveProgressDocument();
            });

            tile.Controls.Add(picture);
            tile.Controls.Add(nameLabel);
            tile.Controls.Add(rarityLabel);

            return tile;
        }

        private void Tile_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var tile = FindTilePanel(sender as Control);
            if (tile?.Tag is not CatTileBinding binding)
            {
                return;
            }

            ShowCatContextMenu(tile, binding);
        }

        private async void ShowCatContextMenu(Control anchor, CatTileBinding binding)
        {
            var menu = new ContextMenuStrip();

            var evolutionMenu = new ToolStripMenuItem("Evolution");
            evolutionMenu.DropDownItems.Add(new ToolStripMenuItem("Loading...") { Enabled = false });

            var wikiItem = new ToolStripMenuItem("Wiki");
            wikiItem.Click += (_, _) => OpenWikiForCat(binding.Category, binding.Entry.Name);

            menu.Items.Add(evolutionMenu);
            menu.Items.Add(wikiItem);

            menu.Show(anchor, anchor.PointToClient(Cursor.Position));

            if (TryGetCachedEvolutionInfo(binding.Category, binding.Entry.Name, out var cachedInfo))
            {
                PopulateEvolutionMenu(evolutionMenu, binding, cachedInfo);
                return;
            }

            var info = await GetEvolutionInfoAsync(binding.Category, binding.Entry.Name);

            if (menu.IsDisposed || evolutionMenu.IsDisposed)
            {
                return;
            }

            PopulateEvolutionMenu(evolutionMenu, binding, info);
        }

        private async Task RefreshAllCatImagesAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(_imageCacheDirectory))
                    {
                        return;
                    }

                    foreach (var file in Directory.EnumerateFiles(_imageCacheDirectory, "v2_*.png", SearchOption.TopDirectoryOnly))
                    {
                        File.Delete(file);
                    }
                });

                RenderCatTiles();
            }
            catch
            {
                MessageBox.Show(this, "Could not refresh images.", "Refresh Images", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PopulateEvolutionMenu(ToolStripMenuItem evolutionMenu, CatTileBinding binding, EvolutionInfo? info)
        {
            evolutionMenu.DropDownItems.Clear();

            if (info is null)
            {
                evolutionMenu.DropDownItems.Add(new ToolStripMenuItem("No evolution data") { Enabled = false });
                return;
            }

            if (!string.IsNullOrWhiteSpace(info.NextEvolutionName))
            {
                var evolveItem = new ToolStripMenuItem(info.NextEvolutionName);
                evolveItem.Click += (_, _) => EvolveCat(binding, info.NextEvolutionName!);
                evolutionMenu.DropDownItems.Add(evolveItem);
            }
            else
            {
                var latestItem = new ToolStripMenuItem("Link zur Evolution");
                latestItem.Click += (_, _) => OpenWikiPage(info.PageTitle);
                evolutionMenu.DropDownItems.Add(latestItem);
            }
        }

        private void EvolveCat(CatTileBinding binding, string nextEvolutionName)
        {
            var oldName = binding.Entry.Name;
            binding.Entry.Name = nextEvolutionName;
            binding.Entry.LevelUnknown = false;
            InvalidateEvolutionInfoCache(binding.Category, oldName);
            InvalidateEvolutionInfoCache(binding.Category, nextEvolutionName);
            SaveProgressDocument();
            RenderCatTiles();
        }

        private void OpenWikiForCat(string category, string catName)
        {
            _ = OpenWikiForCatAsync(category, catName);
        }

        private async Task OpenWikiForCatAsync(string category, string catName)
        {
            var pageTitle = await ResolveBestWikiPageTitleAsync(category, catName) ?? catName;
            OpenWikiPage(pageTitle);
        }

        private static void OpenWikiPage(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/wiki/{Uri.EscapeDataString(pageTitle.Replace(' ', '_'))}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private async Task<EvolutionInfo?> GetEvolutionInfoAsync(string category, string catName)
        {
            if (TryGetCachedEvolutionInfo(category, catName, out var cachedInfo))
            {
                return cachedInfo;
            }

            var key = BuildEvolutionCacheKey(category, catName);
            Task<EvolutionInfo?> task;
            lock (_evolutionCacheLock)
            {
                if (!_evolutionInfoTasks.TryGetValue(key, out task!))
                {
                    task = LoadEvolutionInfoCoreAsync(category, catName);
                    _evolutionInfoTasks[key] = task;
                }
            }

            var info = await task;

            lock (_evolutionCacheLock)
            {
                _evolutionInfoCache[key] = info;
                _evolutionInfoTasks.Remove(key);
            }

            return info;
        }

        private async Task<EvolutionInfo?> LoadEvolutionInfoCoreAsync(string category, string catName)
        {
            await _evolutionLoadSemaphore.WaitAsync();
            try
            {
            var pageTitle = await ResolveBestWikiPageTitleAsync(category, catName);
            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                return null;
            }

            var wikiText = await TryGetWikiTextAsync(pageTitle);
            if (string.IsNullOrWhiteSpace(wikiText))
            {
                return null;
            }

            var names = ParseFormNames(wikiText);
            var currentForm = DetermineDesiredForm(catName, names);
            var next = currentForm switch
            {
                CatForm.Normal when !string.IsNullOrWhiteSpace(names.GetValueOrDefault(CatForm.Evolved)) => names[CatForm.Evolved],
                CatForm.Evolved when !string.IsNullOrWhiteSpace(names.GetValueOrDefault(CatForm.True)) => names[CatForm.True],
                _ => null
            };

            return new EvolutionInfo(pageTitle, next);
            }
            finally
            {
                _evolutionLoadSemaphore.Release();
            }
        }

        private void StartEvolutionPreloadForVisibleCats(IEnumerable<CatTileBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                _ = GetEvolutionInfoAsync(binding.Category, binding.Entry.Name);
            }
        }

        private bool TryGetCachedEvolutionInfo(string category, string catName, out EvolutionInfo? info)
        {
            var key = BuildEvolutionCacheKey(category, catName);
            lock (_evolutionCacheLock)
            {
                return _evolutionInfoCache.TryGetValue(key, out info);
            }
        }

        private void InvalidateEvolutionInfoCache(string category, string catName)
        {
            var key = BuildEvolutionCacheKey(category, catName);
            lock (_evolutionCacheLock)
            {
                _evolutionInfoCache.Remove(key);
            }
        }

        private static string BuildEvolutionCacheKey(string category, string catName)
        {
            return $"{category}|{catName}";
        }

        private async Task RefreshCatImageAsync(CatTileBinding binding)
        {
            try
            {
                var cachePath = Path.Combine(_imageCacheDirectory, BuildSafeFileName($"v2_{binding.Category}_{binding.Entry.Name}") + ".png");
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }

                var refreshedPath = await GetOrDownloadCatImageAsync(binding.Category, binding.Entry.Name);
                if (string.IsNullOrWhiteSpace(refreshedPath))
                {
                    MessageBox.Show(this, "Image refresh failed for this cat.", "Refresh Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                RenderCatTiles();
            }
            catch
            {
                MessageBox.Show(this, "Could not refresh image.", "Refresh Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task<string?> ResolveBestWikiPageTitleAsync(string category, string catName)
        {
            foreach (var candidate in BuildPageCandidates(category, catName))
            {
                var resolved = await TryResolveExistingPageTitleAsync(candidate);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }

            var searchedTitle = await SearchBestPageTitleAsync(catName);
            if (!string.IsNullOrWhiteSpace(searchedTitle))
            {
                return await TryResolveExistingPageTitleAsync(searchedTitle) ?? searchedTitle;
            }

            return null;
        }

        private async Task<string?> TryResolveExistingPageTitleAsync(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&titles={Uri.EscapeDataString(pageTitle)}&redirects=1&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("pages", out var pagesElement))
            {
                return null;
            }

            foreach (var page in pagesElement.EnumerateObject())
            {
                if (page.NameEquals("-1"))
                {
                    continue;
                }

                if (page.Value.TryGetProperty("title", out var titleElement))
                {
                    return titleElement.GetString();
                }
            }

            return null;
        }

        private IEnumerable<CatTileBinding> GetOwnedCatsForView()
        {
            return _progressDocument.Categories
                .OrderBy(x => x.Key)
                .SelectMany(x => x.Value
                    .Where(cat => cat.Owned)
                    .OrderBy(cat => cat.ViewOrder ?? int.MaxValue)
                    .ThenBy(cat => cat.Name)
                    .Select(cat => new CatTileBinding(x.Key, cat)));
        }

        private async Task LoadCatImageAsync(PictureBox pictureBox, string category, string catName)
        {
            try
            {
                var imagePath = await GetOrDownloadCatImageAsync(category, catName);
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    return;
                }

                using var stream = File.OpenRead(imagePath);
                using var image = Image.FromStream(stream);
                var clone = new Bitmap(image);

                if (pictureBox.IsDisposed)
                {
                    clone.Dispose();
                    return;
                }

                if (pictureBox.InvokeRequired)
                {
                    pictureBox.BeginInvoke(new Action(() =>
                    {
                        if (pictureBox.IsDisposed)
                        {
                            clone.Dispose();
                            return;
                        }

                        var oldImage = pictureBox.Image;
                        pictureBox.Image = clone;
                        oldImage?.Dispose();
                    }));
                }
                else
                {
                    var oldImage = pictureBox.Image;
                    pictureBox.Image = clone;
                    oldImage?.Dispose();
                }
            }
            catch
            {
            }
        }

        private async Task<string?> GetOrDownloadCatImageAsync(string category, string catName)
        {
            var cachePath = Path.Combine(_imageCacheDirectory, BuildSafeFileName($"v2_{category}_{catName}") + ".png");
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            await _imageLoadSemaphore.WaitAsync();
            try
            {
                if (File.Exists(cachePath))
                {
                    return cachePath;
                }

                var imageUrl = await ResolveMirahezeImageUrlAsync(category, catName);
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return null;
                }

                var bytes = await _httpClient.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(cachePath, bytes);
                return cachePath;
            }
            catch
            {
                return null;
            }
            finally
            {
                _imageLoadSemaphore.Release();
            }
        }

        private async Task<string?> ResolveMirahezeImageUrlAsync(string category, string catName)
        {
            foreach (var pageTitle in BuildPageCandidates(category, catName))
            {
                var formImageUrl = await TryGetFormAwareImageUrlAsync(pageTitle, catName);
                if (!string.IsNullOrWhiteSpace(formImageUrl))
                {
                    return formImageUrl;
                }

                var thumbUrl = await TryGetPageThumbnailUrlAsync(pageTitle);
                if (!string.IsNullOrWhiteSpace(thumbUrl))
                {
                    return thumbUrl;
                }

                var fileTitle = await TryGetImageFileTitleAsync(pageTitle);
                if (string.IsNullOrWhiteSpace(fileTitle))
                {
                    continue;
                }

                var fileUrl = await TryGetImageFileUrlAsync(fileTitle);
                if (!string.IsNullOrWhiteSpace(fileUrl))
                {
                    return fileUrl;
                }
            }

            var searchedTitle = await SearchBestPageTitleAsync(catName);
            if (!string.IsNullOrWhiteSpace(searchedTitle))
            {
                var formImageUrl = await TryGetFormAwareImageUrlAsync(searchedTitle, catName);
                if (!string.IsNullOrWhiteSpace(formImageUrl))
                {
                    return formImageUrl;
                }

                var thumbUrl = await TryGetPageThumbnailUrlAsync(searchedTitle);
                if (!string.IsNullOrWhiteSpace(thumbUrl))
                {
                    return thumbUrl;
                }
            }

            return null;
        }

        private async Task<string?> TryGetFormAwareImageUrlAsync(string pageTitle, string catName)
        {
            var wikiText = await TryGetWikiTextAsync(pageTitle);
            if (string.IsNullOrWhiteSpace(wikiText))
            {
                return null;
            }

            var names = ParseFormNames(wikiText);
            var desiredForm = DetermineDesiredForm(catName, names);

            var imageFields = ParseFormImageFields(wikiText);
            if (imageFields.TryGetValue(desiredForm, out var explicitImageFile) && !string.IsNullOrWhiteSpace(explicitImageFile))
            {
                var explicitImageUrl = await TryGetImageFileUrlAsync(explicitImageFile);
                if (!string.IsNullOrWhiteSpace(explicitImageUrl))
                {
                    return explicitImageUrl;
                }
            }

            var images = await TryGetPageImageTitlesAsync(pageTitle);
            if (images.Count == 0)
            {
                return null;
            }

            var suffix = desiredForm switch
            {
                CatForm.Normal => "f00",
                CatForm.Evolved => "c00",
                CatForm.True => "s00",
                _ => "f00"
            };

            var preferred = images.FirstOrDefault(x => Regex.IsMatch(x, $@"^File:Uni\d+\s+{suffix}\.(png|webp)$", RegexOptions.IgnoreCase));
            preferred ??= images.FirstOrDefault(x => Regex.IsMatch(x, @"^File:Uni\d+\s+[fcs]00\.(png|webp)$", RegexOptions.IgnoreCase));
            preferred ??= images.FirstOrDefault();

            return string.IsNullOrWhiteSpace(preferred)
                ? null
                : await TryGetImageFileUrlAsync(preferred);
        }

        private async Task<string?> TryGetWikiTextAsync(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=parse&page={Uri.EscapeDataString(pageTitle)}&prop=wikitext&redirects=1&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("parse", out var parseElement) ||
                !parseElement.TryGetProperty("wikitext", out var wikitextElement) ||
                !wikitextElement.TryGetProperty("*", out var valueElement))
            {
                return null;
            }

            return valueElement.GetString();
        }

        private async Task<List<string>> TryGetPageImageTitlesAsync(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&titles={Uri.EscapeDataString(pageTitle)}&redirects=1&prop=images&imlimit=max&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("pages", out var pagesElement))
            {
                return [];
            }

            var images = new List<string>();
            foreach (var page in pagesElement.EnumerateObject())
            {
                if (!page.Value.TryGetProperty("images", out var imagesElement) || imagesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var imageItem in imagesElement.EnumerateArray())
                {
                    if (imageItem.TryGetProperty("title", out var titleElement))
                    {
                        var title = titleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            images.Add(title);
                        }
                    }
                }
            }

            return images;
        }

        private async Task<string?> TryGetPageThumbnailUrlAsync(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&titles={Uri.EscapeDataString(pageTitle)}&redirects=1&prop=pageimages&pithumbsize=320&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("pages", out var pagesElement))
            {
                return null;
            }

            foreach (var page in pagesElement.EnumerateObject())
            {
                if (page.Value.TryGetProperty("thumbnail", out var thumbnailElement) &&
                    thumbnailElement.TryGetProperty("source", out var sourceElement))
                {
                    return sourceElement.GetString();
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildPageCandidates(string category, string catName)
        {
            if (TryGetTitleAliases(catName, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    yield return alias;
                }
            }

            var normalizedRarity = category switch
            {
                "super_rare" => "Super Rare",
                "uber_rare" => "Uber Rare",
                _ => char.ToUpper(category[0]) + category[1..]
            };

            yield return $"{catName} ({normalizedRarity} Cat)";
            yield return catName;

            if (catName.Contains("CC", StringComparison.OrdinalIgnoreCase))
            {
                yield return catName.Replace(" CC", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            }

            if (catName.Contains('β'))
            {
                yield return catName.Replace(" β", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            }
        }

        private static bool TryGetTitleAliases(string catName, out string[] aliases)
        {
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Saber Alter CC"] = ["Saber (Uber Rare Cat)", "Saber"],
                ["Saber Alter Cat"] = ["Saber the Cat (Special Cat)", "Saber Alter Cat"],
                ["Commander Cat"] = ["Lieutenant Cat (Special Cat)", "Commander Cat"],
                ["Major Space Cat"] = ["Major Space Cat", "Space Cat (Rare Cat)"],
                ["Music Fest Thundia β"] = ["Music Fest Thundia (Uber Rare Cat)", "Music Fest Thundia"],
                ["Cameraman Cat"] = ["Cameraman Cat", "Mermaid Cat (Rare Cat)"],
                ["Li'l Mythical Titan Cat"] = ["Li'l Mythical Titan", "Mythical Titan Cat"],
                ["Li'l Sakura CC"] = ["Li'l Sakura (Special Cat)", "Li'l Sakura"],
                ["Li'l Shirou CC"] = ["Li'l Shirou (Rare Cat)", "Li'l Shirou"],
                ["Enchantress"] = ["Enchantress Cat"],
                ["Plane"] = ["Plane Cat"],
                ["Rin Tohsaka CC"] = ["Rin Tohsaka (Uber Rare Cat)"],
                ["Illyasviel CC"] = ["Illyasviel (Uber Rare Cat)"],
                ["Lancer CC"] = ["Lancer (Uber Rare Cat)"],
                ["True Assassin CC"] = ["True Assassin (Uber Rare Cat)"]
            };

            if (map.TryGetValue(catName, out aliases!))
            {
                return true;
            }

            aliases = [];
            return false;
        }

        private async Task<string?> SearchBestPageTitleAsync(string catName)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(catName)}&format=json&srlimit=8";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("search", out var searchElement) ||
                searchElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            string? firstTitle = null;
            foreach (var result in searchElement.EnumerateArray())
            {
                if (!result.TryGetProperty("title", out var titleElement))
                {
                    continue;
                }

                var title = titleElement.GetString();
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                firstTitle ??= title;
                if (title.Contains(" Cat)", StringComparison.OrdinalIgnoreCase) || title.Contains(" Cat", StringComparison.OrdinalIgnoreCase))
                {
                    return title;
                }
            }

            return firstTitle;
        }

        private static Dictionary<CatForm, string> ParseFormNames(string wikiText)
        {
            return new Dictionary<CatForm, string>
            {
                [CatForm.Normal] = ReadTemplateValue(wikiText, "Normal Name") ?? string.Empty,
                [CatForm.Evolved] = ReadTemplateValue(wikiText, "Evolved Name") ?? string.Empty,
                [CatForm.True] = ReadTemplateValue(wikiText, "True Name") ?? string.Empty
            };
        }

        private static Dictionary<CatForm, string> ParseFormImageFields(string wikiText)
        {
            var images = new Dictionary<CatForm, string>();

            var normal = ReadTemplateValue(wikiText, "Normal Image");
            var evolved = ReadTemplateValue(wikiText, "Evolved Image");
            var trueForm = ReadTemplateValue(wikiText, "True Image");

            if (!string.IsNullOrWhiteSpace(normal))
            {
                images[CatForm.Normal] = EnsureFilePrefix(normal);
            }

            if (!string.IsNullOrWhiteSpace(evolved))
            {
                images[CatForm.Evolved] = EnsureFilePrefix(evolved);
            }

            if (!string.IsNullOrWhiteSpace(trueForm))
            {
                images[CatForm.True] = EnsureFilePrefix(trueForm);
            }

            return images;
        }

        private static CatForm DetermineDesiredForm(string catName, Dictionary<CatForm, string> names)
        {
            if (TryGetForcedForm(catName, out var forcedForm))
            {
                return forcedForm;
            }

            if (NameMatches(catName, names.GetValueOrDefault(CatForm.True)))
            {
                return CatForm.True;
            }

            if (NameMatches(catName, names.GetValueOrDefault(CatForm.Evolved)))
            {
                return CatForm.Evolved;
            }

            if (NameMatches(catName, names.GetValueOrDefault(CatForm.Normal)))
            {
                return CatForm.Normal;
            }

            if (catName.Contains("CC", StringComparison.OrdinalIgnoreCase) || catName.Contains('β'))
            {
                return CatForm.Evolved;
            }

            return CatForm.Normal;
        }

        private static bool TryGetForcedForm(string catName, out CatForm form)
        {
            var map = new Dictionary<string, CatForm>(StringComparer.OrdinalIgnoreCase)
            {
                ["Commander Cat"] = CatForm.Evolved,
                ["Major Space Cat"] = CatForm.Evolved
            };

            if (map.TryGetValue(catName, out form))
            {
                return true;
            }

            form = CatForm.Normal;
            return false;
        }

        private static bool NameMatches(string left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            return NormalizeName(left).Equals(NormalizeName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeName(string value)
        {
            return Regex.Replace(value, @"[^a-zA-Z0-9]", string.Empty);
        }

        private static string? ReadTemplateValue(string wikiText, string key)
        {
            var match = Regex.Match(wikiText, $@"\|\s*{Regex.Escape(key)}\s*=\s*(.+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return match.Groups[1].Value.Trim();
        }

        private static string EnsureFilePrefix(string value)
        {
            var cleaned = value.Trim();
            return cleaned.StartsWith("File:", StringComparison.OrdinalIgnoreCase)
                ? cleaned
                : $"File:{cleaned}";
        }

        private async Task<string?> TryGetImageFileTitleAsync(string pageTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&titles={Uri.EscapeDataString(pageTitle)}&redirects=1&prop=images&imlimit=max&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("pages", out var pagesElement))
            {
                return null;
            }

            foreach (var page in pagesElement.EnumerateObject())
            {
                if (!page.Value.TryGetProperty("images", out var imagesElement) || imagesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                string? fallback = null;
                foreach (var imageItem in imagesElement.EnumerateArray())
                {
                    if (!imageItem.TryGetProperty("title", out var titleElement))
                    {
                        continue;
                    }

                    var title = titleElement.GetString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    if (Regex.IsMatch(title, @"^File:Uni\d+.*\.(png|webp)$", RegexOptions.IgnoreCase))
                    {
                        return title;
                    }

                    if (fallback is null && Regex.IsMatch(title, @"\.(png|webp|jpg|jpeg)$", RegexOptions.IgnoreCase))
                    {
                        fallback = title;
                    }
                }

                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    return fallback;
                }
            }

            return null;
        }

        private async Task<string?> TryGetImageFileUrlAsync(string fileTitle)
        {
            var url = $"https://battlecats.miraheze.org/w/api.php?action=query&titles={Uri.EscapeDataString(fileTitle)}&prop=imageinfo&iiprop=url&format=json";
            var json = await _httpClient.GetStringAsync(url);

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("query", out var queryElement) ||
                !queryElement.TryGetProperty("pages", out var pagesElement))
            {
                return null;
            }

            foreach (var page in pagesElement.EnumerateObject())
            {
                if (!page.Value.TryGetProperty("imageinfo", out var imageInfoElement) || imageInfoElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var info in imageInfoElement.EnumerateArray())
                {
                    if (info.TryGetProperty("url", out var urlElement))
                    {
                        return urlElement.GetString();
                    }
                }
            }

            return null;
        }

        private static string BuildSafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var safe = value;
            foreach (var character in invalid)
            {
                safe = safe.Replace(character, '_');
            }

            return safe;
        }

        private enum CatForm
        {
            Normal,
            Evolved,
            True
        }

        private void AddLevelRow(Panel tile, string labelText, int top, int value, Action<int> onValueChanged)
        {
            var label = new Label
            {
                Left = 8,
                Top = top + 4,
                Width = 36,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = labelText
            };

            var minus = new Button
            {
                Left = 44,
                Top = top,
                Width = 24,
                Height = 26,
                Text = "-"
            };

            var input = new NumericUpDown
            {
                Left = 72,
                Top = top,
                Width = 72,
                Height = 26,
                Minimum = 0,
                Maximum = 200,
                Value = value,
                TextAlign = HorizontalAlignment.Center
            };

            var plus = new Button
            {
                Left = 148,
                Top = top,
                Width = 52,
                Height = 26,
                Text = "+"
            };

            var suppressEvent = false;

            minus.Click += (_, _) =>
            {
                if (input.Value <= input.Minimum)
                {
                    return;
                }

                suppressEvent = true;
                input.Value -= 1;
                suppressEvent = false;
                onValueChanged((int)input.Value);
            };

            plus.Click += (_, _) =>
            {
                if (input.Value >= input.Maximum)
                {
                    return;
                }

                suppressEvent = true;
                input.Value += 1;
                suppressEvent = false;
                onValueChanged((int)input.Value);
            };

            input.ValueChanged += (_, _) =>
            {
                if (!suppressEvent)
                {
                    onValueChanged((int)input.Value);
                }
            };

            tile.Controls.Add(label);
            tile.Controls.Add(minus);
            tile.Controls.Add(input);
            tile.Controls.Add(plus);
        }

        private static Image CreatePlaceholderImage(string name)
        {
            var bitmap = new Bitmap(170, 120);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.WhiteSmoke);
            using var pen = new Pen(Color.Silver);
            graphics.DrawRectangle(pen, 0, 0, bitmap.Width - 1, bitmap.Height - 1);
            using var textBrush = new SolidBrush(Color.DimGray);
            using var textFont = new Font(SystemFonts.MessageBoxFont.FontFamily, 8, FontStyle.Regular);
            var textRect = new RectangleF(8, 8, bitmap.Width - 16, bitmap.Height - 16);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(name, textFont, textBrush, textRect, format);
            return bitmap;
        }

        private void ConfigureCampaignGrid()
        {
            gridCampaignProgress.AutoGenerateColumns = false;
            gridCampaignProgress.Columns.Clear();

            gridCampaignProgress.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Campaign",
                HeaderText = "Campaign",
                DataPropertyName = nameof(CampaignProgressRow.Campaign),
                ReadOnly = true,
                Width = 240
            });

            gridCampaignProgress.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Chapter",
                HeaderText = "Chapter",
                DataPropertyName = nameof(CampaignProgressRow.Chapter),
                Width = 220
            });

            gridCampaignProgress.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Stage",
                HeaderText = "Subchapter/Stage",
                DataPropertyName = nameof(CampaignProgressRow.Stage),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            btnSaveProgress.Click += BtnSaveProgress_Click;
            gridCampaignProgress.DataSource = _campaignRows;
        }

        private void BindCampaignProgress()
        {
            _campaignRows.Clear();
            foreach (var entry in _progressDocument.CampaignProgress ?? ProgressDocumentService.CreateDefaultCampaignProgress())
            {
                _campaignRows.Add(new CampaignProgressRow(entry));
            }
        }

        private static string FormatCategoryName(string category)
        {
            return category.Replace("_", " ");
        }

        private static Color GetCategoryColor(string category)
        {
            return category.ToLowerInvariant() switch
            {
                "normal" => Color.FromArgb(232, 242, 255),
                "special" => Color.FromArgb(237, 255, 238),
                "rare" => Color.FromArgb(255, 250, 230),
                "super_rare" => Color.FromArgb(255, 239, 225),
                "uber_rare" => Color.FromArgb(246, 233, 255),
                _ => Color.White
            };
        }

        private void BtnSaveProgress_Click(object? sender, EventArgs e)
        {
            gridCampaignProgress.EndEdit();
            _progressDocument.CampaignProgress = _campaignRows
                .Select(x => new CampaignProgressEntry
                {
                    Campaign = x.Campaign,
                    Chapter = x.Chapter,
                    Stage = x.Stage
                })
                .ToList();

            SaveProgressDocument();
            MessageBox.Show(this, "Campaign progress saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadProgressDocument()
        {
            _progressFilePath = ProgressDocumentService.ResolveProgressFilePath();
            _progressDocument = _progressDocumentService.Load(_progressFilePath);
        }

        private void SaveProgressDocument()
        {
            _progressDocumentService.Save(_progressFilePath, _progressDocument);
        }

    }
}
