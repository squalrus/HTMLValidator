## Update

This repository is not longer maintained. Code was moved elsewhere.

## Debugging

Generate `local.settings.json`

```
func azure functionapp fetch-app-settings htmlvalidator
```

Specify Functions to run

```json
{
    "version": "2.0",
    "functions": [
        "ParseSitemap"
    ]
}
```

Run function in `RunOnStartup` mode (not for production use)

```csharp
[TimerTrigger("0 0 0 * * *", RunOnStartup = true)] TimerInfo myTimer,
```
