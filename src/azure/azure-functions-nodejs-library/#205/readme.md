# LanguageWorkerConsoleLog[error] Worker uncaught exception (learn more: https://go.microsoft.com/fwlink/?linkid=2097909 ): TypeError: Cannot redefine property: message

This repo is a reproduction of 
https://github.com/Azure/azure-functions-nodejs-library/issues/205

It uses zod in global context to parse settings from `process.env`.

## steps to reproduce
run
```bash
npm install
npm run start
```

## **expected**

A zod error should pop up somewhere in the logs.

## **actual**

Various logs are written, then the func appears to be started, but stops after a while.
Final logs contain:

```bash
[error] Worker uncaught exception (learn more: https://go.microsoft.com/fwlink/?linkid=2097909 ): TypeError: Cannot redefine property: message     at Function.defineProperty (<anonymous>)     at t.ensureErrorType (/opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:31119)     at /opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:53526     at Generator.throw (<anonymous>)     at a (/opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:51940)     at process.processTicksAndRejections (node:internal/process/task_queues:95:5)

Language Worker Process exited. Pid=39346.

node exited with code 1 (0x1). LanguageWorkerConsoleLog[error] Worker uncaught exception (learn more: https://go.microsoft.com/fwlink/?linkid=2097909 ): TypeError: Cannot redefine property: message     at Function.defineProperty (<anonymous>)     at t.ensureErrorType (/opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:31119)     at /opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:53526     at Generator.throw (<anonymous>)     at a (/opt/homebrew/Cellar/azure-functions-core-tools@4/4.0.5907/workers/node/dist/src/worker-bundle.js:2:51940)     at process.processTicksAndRejections (node:internal/process/task_queues:95:5).

Exceeded language worker restart retry count for runtime:node. Shutting down and proactively recycling the Functions Host to recover
```

## **workaround**
Wrapping the causing logic into a try/catch and `console.error`, then rethrow a new Error, to at least see the original error in the console.

## **notes**
Adding `"Value"="FooBar"` to `local.settings.json` or running `npm run start:nocrash` works, as no error is thrown upon `zod.parsing`.




