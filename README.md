# Dart.Weather.Api - Historical Weather Fetcher (Open‑Meteo)

This service:
- Reads dates from `dates.txt`
- Normalizes multiple date formats into ISO (`yyyy-MM-dd`)
- Uses the Open‑Meteo Historical Weather API (no API key) to fetch daily fields for Dallas, TX:
  - `temperature_2m_min`
  - `temperature_2m_max`
  - `precipitation_sum`
- Saves per-date JSON under `weather-data/{yyyy-MM-dd}.json`
- Exposes `GET /api/weather` returning one entry per input date with:
  - `Date` (normalized)
  - `MinTemperatureC`, `MaxTemperatureC`, `PrecipitationMm`
  - `Status` (OK or error message)
  - `SourceFile` (path to local saved JSON when present)

Notes:
- Invalid dates (e.g., April 31) are recorded with an error in `Status`.
- Existing saved JSON files are reused to avoid unnecessary API calls.

How to wire up
1. Register services in `Program.cs`:

   - Add these registrations to the DI container (example shown below).

2. Ensure `dates.txt` is at the content root of the application (project root when running).

3. Run the app and call:
   - `GET /api/weather`

Program.cs snippet to add (example):