$ErrorActionPreference = 'Stop'
$j = Get-Content 'Progress.json' -Raw | ConvertFrom-Json

$cats = @()
foreach ($catProp in $j.Categories.PSObject.Properties) {
	$rarity = $catProp.Name
	foreach ($c in $catProp.Value) {
		$cats += [pscustomobject]@{ Category = $rarity; Name = $c.Name }
	}
}

function Normalize-Name([string]$s) {
	if ([string]::IsNullOrWhiteSpace($s)) { return '' }
	return (($s.ToLowerInvariant() -replace '[^a-z0-9]', ''))
}

$uncertain = @()
foreach ($c in $cats) {
	$name = $c.Name
	$expected = $c.Category

	$directUrl = "https://battlecats.miraheze.org/w/api.php?action=query&titles=$([uri]::EscapeDataString($name))&redirects=1&prop=categories&cllimit=500&format=json"
	$direct = Invoke-RestMethod -Uri $directUrl -Method Get -TimeoutSec 40
	$page = $direct.query.pages.PSObject.Properties.Value | Select-Object -First 1

	$resolvedTitle = $null
	$rarityCats = @()
	$confidence = 0.0
	$reason = ''

	if ($page -and $page.pageid -ne -1) {
		$resolvedTitle = $page.title
		if ($page.categories) {
			$rarityCats = @($page.categories | ForEach-Object { $_.title } | Where-Object { $_ -match '^Category:(Normal|Special|Rare|Super Rare|Uber Rare) Cats$' })
		}

		$normIn = Normalize-Name $name
		$normOut = Normalize-Name $resolvedTitle

		if ($normIn -eq $normOut) {
			$confidence = 1.0
			$reason = 'direct exact/redirect'
		}
		elseif ($normOut -like "*$normIn*" -or $normIn -like "*$normOut*") {
			$confidence = 0.92
			$reason = 'direct close title'
		}
		else {
			$confidence = 0.82
			$reason = 'direct different title'
		}
	}
	else {
		$searchUrl = "https://battlecats.miraheze.org/w/api.php?action=query&list=search&srsearch=$([uri]::EscapeDataString($name))&format=json&srlimit=5"
		$search = Invoke-RestMethod -Uri $searchUrl -Method Get -TimeoutSec 40
		$hit = @($search.query.search)[0]

		if ($hit) {
			$resolvedTitle = $hit.title
			$normIn = Normalize-Name $name
			$normOut = Normalize-Name $resolvedTitle

			if ($normOut -like "*$normIn*" -or $normIn -like "*$normOut*") {
				$confidence = 0.72
				$reason = 'search close title'
			}
			else {
				$confidence = 0.6
				$reason = 'search different title'
			}
		}
		else {
			$resolvedTitle = '<no hit>'
			$confidence = 0.0
			$reason = 'no wiki hit'
		}
	}

	if ($confidence -lt 1.0) {
		$uncertain += [pscustomobject]@{
			Category = $expected
			Name = $name
			ResolvedTitle = $resolvedTitle
			RarityCats = [string]::Join('|', $rarityCats)
			Confidence = [math]::Round($confidence, 2)
			Reason = $reason
		}
	}

	Start-Sleep -Milliseconds 40
}

$uncertain | Sort-Object Confidence, Name | ConvertTo-Json -Depth 4 | Set-Content '.tmp-name-verification-uncertain.json' -Encoding UTF8
Write-Output ("UNCERTAIN_COUNT=" + $uncertain.Count)
