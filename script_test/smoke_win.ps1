#requires -Version 5.1
param(
  [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = 'Stop'
function Line($m){ Write-Host "`n==== $m ====" }

function HasCmd($name){ $null -ne (Get-Command $name -ErrorAction SilentlyContinue) }
$Curl = if (HasCmd 'curl.exe') { 'curl.exe' } elseif (HasCmd 'curl') { 'curl' } else { throw 'curl not found. Install curl or run from Win10+ where curl.exe is available.' }
$Jq   = if (HasCmd 'jq.exe') { 'jq.exe' } elseif (HasCmd 'jq') { 'jq' } else { $null }
$UseJq = $null -ne $Jq
if (-not $UseJq) { Write-Host 'jq not found -- output will be raw JSON/PowerShell formatted.' }

function Show-Json([string]$json){
  if ($UseJq) { $json | & $Jq '.' } else { $obj = $json | ConvertFrom-Json; $obj | ConvertTo-Json -Depth 8 }
}

function Invoke-Curl{
  param(
    [string[]]$ExtraHeaders = @(),
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
  )

  $processed = New-Object System.Collections.Generic.List[string]
  $tempFiles = @()
  $i = 0
  while ($i -lt $Args.Count) {
    $arg = $Args[$i]
    if ($arg -in @('-d','--data','--data-raw','--data-binary')) {
      if ($i + 1 -ge $Args.Count) { throw "Missing value for $arg payload argument." }
      $payload = $Args[$i + 1]
      $tmp = [System.IO.Path]::GetTempFileName()
      Set-Content -Path $tmp -Value $payload -Encoding UTF8 -NoNewline
      $processed.Add('--data-binary')
      $processed.Add("@$tmp")
      $tempFiles += $tmp
      $i += 2
      continue
    }

    $processed.Add($arg)
    $i++
  }

  $headerArgs = foreach ($h in $ExtraHeaders) { '-H'; $h }
  $commandArgs = @('-sS','-k') + $headerArgs + $processed.ToArray()
  if ($env:DEBUG_CURL -eq '1') {
    Write-Host ("[Invoke-Curl] raw args={0}" -f ($Args -join ' | '))
    Write-Host ("[curl] {0} {1}" -f $Curl, ($commandArgs -join ' '))
  }
  try {
    & $Curl @commandArgs
  }
  finally {
    foreach ($tmp in $tempFiles) {
      Remove-Item -Path $tmp -ErrorAction SilentlyContinue
    }
  }
}

function Curl-Json(){
  if ($env:DEBUG_CURL -eq '1') {
    Write-Host ("[Curl-Json] args={0}" -f ($args -join ' | '))
  }
  Invoke-Curl -ExtraHeaders @('Content-Type: application/json') @args
}
function Curl-AuthJson(){ 
  param([string]$token) 
  if ($env:DEBUG_CURL -eq '1') {
    $tokenPreview = if ([string]::IsNullOrEmpty($token)) { '<none>' } elseif ($token.Length -le 12) { $token } else { "{0}..." -f $token.Substring(0,12) }
    Write-Host ("[Curl-AuthJson] token={0} args={1}" -f $tokenPreview, ($args -join ' | '))
  }
  Invoke-Curl -ExtraHeaders @("Authorization: Bearer $token",'Content-Type: application/json') @args 
}
function Curl-AuthForm(){ param([string]$token) & $Curl -sS -k -H "Authorization: Bearer $token" @args }

Line "Auth: login admin"
$resp = Curl-Json '-X' 'POST' "$BaseUrl/api/auth/login" '-d' '{"email":"admin@cinema.com","password":"admin123"}'
$ADMIN_TOKEN = if ($UseJq) { $resp | & $Jq -r '.token' } else { (ConvertFrom-Json $resp).token }
if ([string]::IsNullOrEmpty($ADMIN_TOKEN)) {
  throw "Failed to acquire admin token. Response:`n$resp"
}
Write-Host ("ADMIN_TOKEN: {0}..." -f $ADMIN_TOKEN.Substring(0, [Math]::Min(20,$ADMIN_TOKEN.Length)))

Line "Auth: login user"
$resp = Curl-Json '-X' 'POST' "$BaseUrl/api/auth/login" '-d' '{"email":"user@cinema.com","password":"user123"}'
$USER_TOKEN = if ($UseJq) { $resp | & $Jq -r '.token' } else { (ConvertFrom-Json $resp).token }
if ([string]::IsNullOrEmpty($USER_TOKEN)) {
  throw "Failed to acquire user token. Response:`n$resp"
}
Write-Host ("USER_TOKEN: {0}..." -f $USER_TOKEN.Substring(0, [Math]::Min(20,$USER_TOKEN.Length)))

Line "GET /api/movies"
$resp = Curl-Json "$BaseUrl/api/movies"; Show-Json $resp

Line "GET /api/series"
$resp = Curl-Json "$BaseUrl/api/series"; Show-Json $resp

Line "POST /api/movies (Admin) - create Interstellar"
$resp = Curl-AuthJson $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/movies" '-d' '{"title":"Interstellar","description":"Explorers travel through a wormhole in space.","releaseYear":2014,"durationMinutes":169,"director":"Christopher Nolan","genre":"Sci-Fi"}'
Show-Json $resp
$MOVIE_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }
Write-Host "MOVIE_ID=$MOVIE_ID"

Line "POST /api/movies/$MOVIE_ID/posters (Admin) - by URL"
$resp = Curl-AuthJson $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/movies/$MOVIE_ID/posters" '-d' '{"url":"https://via.placeholder.com/600x900.png","mimeType":"image/png"}'
Show-Json $resp

Line "Upload poster file for movie"
$tmp = [System.IO.Path]::GetTempFileName().Replace('.tmp','.png')
[IO.File]::WriteAllBytes($tmp, [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO0lBUsAAAAASUVORK5CYII='))
$resp = Curl-AuthForm $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/movies/$MOVIE_ID/posters/upload" '-F' "file=@$tmp;type=image/png"
Show-Json $resp
$POSTER_FILE_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }
$POSTER_FILE_URL = if ($UseJq) { $resp | & $Jq -r '.url' } else { (ConvertFrom-Json $resp).url }
Write-Host "POSTER_FILE_ID=$POSTER_FILE_ID"; Write-Host "POSTER_FILE_URL=$POSTER_FILE_URL"

Line "HEAD static poster file (should be 200)"
& $Curl -sS -k -I ("{0}{1}" -f $BaseUrl,$POSTER_FILE_URL) | Select-Object -First 1 | Write-Host

Line "GET /api/posters/$POSTER_FILE_ID/file (stream)"
$downloadTmp = [System.IO.Path]::GetTempFileName()
& $Curl -sS -k -o $downloadTmp -w "HTTP %{http_code}`n" "$BaseUrl/api/posters/$POSTER_FILE_ID/file"
Remove-Item -Force $downloadTmp

Line "GET /api/movies/$MOVIE_ID"
$resp = Curl-Json "$BaseUrl/api/movies/$MOVIE_ID"; Show-Json $resp

Line "POST /api/series (Admin) - create Chernobyl"
$resp = Curl-AuthJson $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/series" '-d' '{"title":"Chernobyl","description":"The story of the 1986 nuclear accident.","releaseYear":2019,"genre":"Drama"}'
Show-Json $resp
$SERIES_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }
Write-Host "SERIES_ID=$SERIES_ID"

Line "POST /api/series/$SERIES_ID/episodes (Admin)"
$resp = Curl-AuthJson $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/series/$SERIES_ID/episodes" '-d' '{"seasonNumber":1,"episodeNumber":1,"title":"1:23:45","description":"First episode","duration":65}'
Show-Json $resp
$EPISODE_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }
Write-Host "EPISODE_ID=$EPISODE_ID"

Line "GET /api/series/$SERIES_ID/episodes"
$resp = Curl-Json "$BaseUrl/api/series/$SERIES_ID/episodes"; Show-Json $resp

Line "Upload poster file for series"
$resp = Curl-AuthForm $ADMIN_TOKEN '-X' 'POST' "$BaseUrl/api/series/$SERIES_ID/posters/upload" '-F' "file=@$tmp;type=image/png"
Show-Json $resp
$SERIES_POSTER_URL = if ($UseJq) { $resp | & $Jq -r '.url' } else { (ConvertFrom-Json $resp).url }
& $Curl -sS -k -I ("{0}{1}" -f $BaseUrl,$SERIES_POSTER_URL) | Select-Object -First 1 | Write-Host

Line "POST /api/reviews (User) - create review for movie"
$reviewCreateBody = @{ movieId = [int]$MOVIE_ID; text = "Amazing!"; rating = 9 } | ConvertTo-Json -Compress
$resp = Curl-AuthJson $USER_TOKEN '-X' 'POST' "$BaseUrl/api/reviews" '-d' $reviewCreateBody
Show-Json $resp
$REVIEW_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }
Write-Host "REVIEW_ID=$REVIEW_ID"

Line "PUT /api/reviews/$REVIEW_ID (User) - update"
$reviewUpdateBody = @{ movieId = [int]$MOVIE_ID; text = "Even better on rewatch"; rating = 10 } | ConvertTo-Json -Compress
$resp = Curl-AuthJson $USER_TOKEN '-X' 'PUT' "$BaseUrl/api/reviews/$REVIEW_ID" '-d' $reviewUpdateBody
Show-Json $resp

Line "GET /api/reviews/movies/$MOVIE_ID"
$resp = Curl-Json "$BaseUrl/api/reviews/movies/$MOVIE_ID"; Show-Json $resp

Line "POST /api/favorites (User) - add movie to favorites"
$favMovieBody = @{ movieId = [int]$MOVIE_ID } | ConvertTo-Json -Compress
$resp = Curl-AuthJson $USER_TOKEN '-X' 'POST' "$BaseUrl/api/favorites" '-d' $favMovieBody
Show-Json $resp
$FAV_MOVIE_ID = if ($UseJq) { $resp | & $Jq -r '.id' } else { (ConvertFrom-Json $resp).id }

Line "POST /api/favorites (User) - add series to favorites"
$favSeriesBody = @{ seriesId = [int]$SERIES_ID } | ConvertTo-Json -Compress
$resp = Curl-AuthJson $USER_TOKEN '-X' 'POST' "$BaseUrl/api/favorites" '-d' $favSeriesBody
Show-Json $resp

Line "GET /api/favorites/me (User)"
$resp = Curl-AuthJson $USER_TOKEN "$BaseUrl/api/favorites/me"; Show-Json $resp

Line "DELETE /api/favorites/$FAV_MOVIE_ID (User)"
try { Curl-AuthJson $USER_TOKEN '-X' 'DELETE' "$BaseUrl/api/favorites/$FAV_MOVIE_ID" | Out-Null } catch {}

Line "GET /api/favorites/me (User) after delete"
$resp = Curl-AuthJson $USER_TOKEN "$BaseUrl/api/favorites/me"; Show-Json $resp

Remove-Item -Force $tmp
Line "Done"
