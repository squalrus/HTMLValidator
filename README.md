[![Build Status](https://squalrus.visualstudio.com/htmlvalidator/_apis/build/status/HTMLValidator%20Production?branchName=master)](https://squalrus.visualstudio.com/htmlvalidator/_build/latest?definitionId=12&branchName=master)

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
