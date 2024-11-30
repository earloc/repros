# repro
Potential request spamming after initial cooldown surpassed
https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137

Showcases an issue, where refresh-intervals get ignored, after the intial refresh-interval has been surpassed, w/o any change occuring on sentinel-keys.
```
  https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137
    ✓ honors refresh-cooldown in "v1.1.2" (1141 ms)
    ✓ honors refresh-cooldown in "v1.1.0" (1119 ms)
    ✕ honors refresh-cooldown in "v1.0.1" (3861 ms)
    ✕ honors refresh-cooldown in "v1.0.0" (3799 ms)
    ✕ honors refresh-cooldown in "v1.0.0-preview4" (3817 ms)
    ✕ honors refresh-cooldown in "v1.0.0-preview3" (3787 ms)
```