# repros

## https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137
Potential request spamming after initial cooldown surpassed

> see [#137.spec.ts](#137.spec.ts)

Showcases an issue, where refresh-intervals get ignored, after the intial refresh-interval has been surpassed and no changes occur on watched sentinel-keys.
```
  https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137
    ✓ honors refresh-cooldown in "v1.1.2" (1141 ms)
    ✓ honors refresh-cooldown in "v1.1.0" (1119 ms)
    ✕ honors refresh-cooldown in "v1.0.1" (3861 ms)
    ✕ honors refresh-cooldown in "v1.0.0" (3799 ms)
    ✕ honors refresh-cooldown in "v1.0.0-preview4" (3817 ms)
    ✕ honors refresh-cooldown in "v1.0.0-preview3" (3787 ms)


```

```bash
https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137 › honors refresh-cooldown in "v1.0.0-preview4"

    expect(received).toBe(expected) // Object.is equality

    Expected: 1
    Received: 1000

      77 |         await settings.refresh();
      78 |       }
    > 79 |
         | ^
      80 |       expect(getSentinelRequestCount).toBe(1);
      81 |
      82 |   }, 10000)

      at #137.spec.ts:79:39
      at fulfilled (#137.spec.ts:38:58)
```