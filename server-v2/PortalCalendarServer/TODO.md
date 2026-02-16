# TODO

- [ ] FIX: remove default "en-US" locale for dates. If possible use the invariant culture everywhere as default.
- [ ] Is the wake up (redraw) crontab format for the client being parsed and interpreted correctly?
- [ ] Check if the "alive check" works
- [ ] Differentiate between 3color "BWY" and "BWR" ePapers
- [ ] Add unit tests for the new code
- [ ] Check all remaining TODOs and FIXMEs in the codebase and either fix them or move them to this list.
- [ ] Remove the legacy perl once it's not needed
- [ ] Check appsettings, maybe make some of it overridable by environment variables so that it can be configured on the prod server
- [ ] Check if the API endpoints have URLs as expected by the current version of client ePapers
- [ ] Is MQTT integration working?
- [ ] Is Telegram integration working?
- [ ] Is OpenWeather integration working?
- [ ] For GoogleFit integration: the callback URL should not be configurable freely, it should be fixed to something like `https://<server>/googlefit/callback` generated automatically in javascript based on the client view of the URL
- [ ] Add an .md for Copilot so that I don't need to repeatedly describe the expected structure and rules.
- [ ] Create semi-automated deployment scripts for the prod server, maybe using Ansible or something similar.
- [ ] Remove failsafes like null checks and catch the exceptions instead when rendering display page.
- [ ] On any error return a universal "⚠️ Error occurred, check the logs" page/image to the client.

# Done

- [X] Remove methods which return plain text "UTC" or "en-US" and replace them with methods that return a CultureInfo or TimeZoneInfo object instead.
- [X] Fix inconsistent timezone handling in templates.