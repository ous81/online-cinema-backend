#!/usr/bin/env bash
set -euo pipefail

API="${BASE_URL:-http://localhost:5000}"
echo "Using API base: $API"

have_jq=true
if ! command -v jq >/dev/null 2>&1; then
  echo "jq not found — output will be raw JSON. Install with: brew install jq"
  have_jq=false
fi

line() { printf '\n==== %s ====\n' "$*"; }
show_json() {
  if $have_jq; then jq .; else cat; fi
}

curl_json() { curl -sS -k -H "Content-Type: application/json" "$@"; }
curl_auth_json() { local token="$1"; shift; curl -sS -k -H "Authorization: Bearer $token" -H "Content-Type: application/json" "$@"; }
curl_auth_form() { local token="$1"; shift; curl -sS -k -H "Authorization: Bearer $token" "$@"; }

# 0) Login as admin and user
line "Auth: login admin"
ADMIN_TOKEN=$(curl_json -X POST "$API/api/auth/login" -d '{"email":"admin@cinema.com","password":"admin123"}' | { $have_jq && jq -r .token || python3 -c 'import sys,json; print(json.load(sys.stdin)["token"])'; })
echo "ADMIN_TOKEN: ${ADMIN_TOKEN:0:20}..."

line "Auth: login user"
USER_TOKEN=$(curl_json -X POST "$API/api/auth/login" -d '{"email":"user@cinema.com","password":"user123"}' | { $have_jq && jq -r .token || python3 -c 'import sys,json; print(json.load(sys.stdin)["token"])'; })
echo "USER_TOKEN: ${USER_TOKEN:0:20}..."

# 1) Lists
line "GET /api/movies"
curl_json "$API/api/movies" | show_json

line "GET /api/series"
curl_json "$API/api/series" | show_json

# 2) Admin creates a movie
line "POST /api/movies (Admin) - create Interstellar"
CREATE_MOVIE_RESP=$(curl_auth_json "$ADMIN_TOKEN" -X POST "$API/api/movies" -d '{"title":"Interstellar","description":"Explorers travel through a wormhole in space.","releaseYear":2014,"durationMinutes":169,"director":"Christopher Nolan","genre":"Sci-Fi"}')
echo "$CREATE_MOVIE_RESP" | show_json
MOVIE_ID=$(echo "$CREATE_MOVIE_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })
echo "MOVIE_ID=$MOVIE_ID"

# 3) Add movie poster by URL (Admin)
line "POST /api/movies/$MOVIE_ID/posters (Admin) - by URL"
MOVIE_POSTER_URL_RESP=$(curl_auth_json "$ADMIN_TOKEN" -X POST "$API/api/movies/$MOVIE_ID/posters" -d '{"url":"https://via.placeholder.com/600x900.png","mimeType":"image/png"}')
echo "$MOVIE_POSTER_URL_RESP" | show_json

# 4) Upload movie poster file (Admin)
line "Upload poster file for movie"
TMP_PNG="$(mktemp /tmp/poster.XXXXXX.png)"
echo "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO0lBUsAAAAASUVORK5CYII=" | base64 --decode > "$TMP_PNG"
MOVIE_UPLOAD_RESP=$(curl_auth_form "$ADMIN_TOKEN" -X POST "$API/api/movies/$MOVIE_ID/posters/upload" -F "file=@$TMP_PNG;type=image/png")
echo "$MOVIE_UPLOAD_RESP" | show_json
MOVIE_FILE_POSTER_ID=$(echo "$MOVIE_UPLOAD_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })
MOVIE_FILE_POSTER_URL=$(echo "$MOVIE_UPLOAD_RESP" | { $have_jq && jq -r .url || python3 -c 'import sys,json; print(json.load(sys.stdin)["url"])'; })
echo "POSTER_FILE_ID=$MOVIE_FILE_POSTER_ID"
echo "POSTER_FILE_URL=$MOVIE_FILE_POSTER_URL"

line "HEAD static poster file (should be 200)"
curl -sS -k -I "$API$MOVIE_FILE_POSTER_URL" | head -n 1

line "GET /api/posters/$MOVIE_FILE_POSTER_ID/file (stream)"
curl -sS -k "$API/api/posters/$MOVIE_FILE_POSTER_ID/file" -o /dev/null -w "HTTP %{http_code}\n"

# 5) Movie details
line "GET /api/movies/$MOVIE_ID"
curl_json "$API/api/movies/$MOVIE_ID" | show_json

# 6) Admin creates a series
line "POST /api/series (Admin) - create Chernobyl"
CREATE_SERIES_RESP=$(curl_auth_json "$ADMIN_TOKEN" -X POST "$API/api/series" -d '{"title":"Chernobyl","description":"The story of the 1986 nuclear accident.","releaseYear":2019,"genre":"Drama"}')
echo "$CREATE_SERIES_RESP" | show_json
SERIES_ID=$(echo "$CREATE_SERIES_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })
echo "SERIES_ID=$SERIES_ID"

# 7) Admin adds episode to series
line "POST /api/series/$SERIES_ID/episodes (Admin)"
CREATE_EP_RESP=$(curl_auth_json "$ADMIN_TOKEN" -X POST "$API/api/series/$SERIES_ID/episodes" -d '{"seasonNumber":1,"episodeNumber":1,"title":"1:23:45","description":"First episode","duration":65}')
echo "$CREATE_EP_RESP" | show_json
EPISODE_ID=$(echo "$CREATE_EP_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })
echo "EPISODE_ID=$EPISODE_ID"

line "GET /api/series/$SERIES_ID/episodes"
curl_json "$API/api/series/$SERIES_ID/episodes" | show_json

# 8) Upload series poster (Admin)
line "Upload poster file for series"
SERIES_UPLOAD_RESP=$(curl_auth_form "$ADMIN_TOKEN" -X POST "$API/api/series/$SERIES_ID/posters/upload" -F "file=@$TMP_PNG;type=image/png")
echo "$SERIES_UPLOAD_RESP" | show_json
SERIES_POSTER_URL=$(echo "$SERIES_UPLOAD_RESP" | { $have_jq && jq -r .url || python3 -c 'import sys,json; print(json.load(sys.stdin)["url"])'; })
curl -sS -k -I "$API$SERIES_POSTER_URL" | head -n 1

# 9) User adds review to the movie
line "POST /api/reviews (User) - create review for movie"
CREATE_REVIEW_RESP=$(curl_auth_json "$USER_TOKEN" -X POST "$API/api/reviews" -d "{\"movieId\":$MOVIE_ID,\"text\":\"Amazing!\",\"rating\":9}")
echo "$CREATE_REVIEW_RESP" | show_json
REVIEW_ID=$(echo "$CREATE_REVIEW_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })
echo "REVIEW_ID=$REVIEW_ID"

line "PUT /api/reviews/$REVIEW_ID (User) - update"
curl_auth_json "$USER_TOKEN" -X PUT "$API/api/reviews/$REVIEW_ID" -d "{\"movieId\":$MOVIE_ID,\"text\":\"Even better on rewatch\",\"rating\":10}" | show_json

line "GET /api/reviews/movies/$MOVIE_ID"
curl_json "$API/api/reviews/movies/$MOVIE_ID" | show_json

# 10) Favorites flow (User)
line "POST /api/favorites (User) - add movie to favorites"
CREATE_FAV_MOVIE_RESP=$(curl_auth_json "$USER_TOKEN" -X POST "$API/api/favorites" -d "{\"movieId\":$MOVIE_ID}")
echo "$CREATE_FAV_MOVIE_RESP" | show_json
FAV_MOVIE_ID=$(echo "$CREATE_FAV_MOVIE_RESP" | { $have_jq && jq -r .id || python3 -c 'import sys,json; print(json.load(sys.stdin)["id"])'; })

line "POST /api/favorites (User) - add series to favorites"
CREATE_FAV_SERIES_RESP=$(curl_auth_json "$USER_TOKEN" -X POST "$API/api/favorites" -d "{\"seriesId\":$SERIES_ID}")
echo "$CREATE_FAV_SERIES_RESP" | show_json

line "GET /api/favorites/me (User)"
curl_auth_json "$USER_TOKEN" "$API/api/favorites/me" | show_json

line "DELETE /api/favorites/$FAV_MOVIE_ID (User)"
curl_auth_json "$USER_TOKEN" -X DELETE "$API/api/favorites/$FAV_MOVIE_ID" >/dev/null || true

line "GET /api/favorites/me (User) after delete"
curl_auth_json "$USER_TOKEN" "$API/api/favorites/me" | show_json

rm -f "$TMP_PNG"
line "Done"

