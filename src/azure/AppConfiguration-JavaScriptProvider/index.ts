export function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

export function createOptions(options: IOptions) {
  return {
    clientOptions: {
      allowInsecureConnection: true
    },
    selectors: [
      {
        keyFilter: options.keyFilter,
        labelFilter: options.labelFilter
      }
    ],
    refreshOptions: {
      enabled: true,
      refreshIntervalInMs: options.refreshIntervalInMs,
      watchedSettings: [{ key: options.sentinelKeyName, label: options.labelFilter }]
    }
  };
}

export interface IOptions {
  sentinelKeyName: string;
  refreshIntervalInMs: number;
  keyFilter: string;
  labelFilter: string;
}

export interface IRefreshable {
  refresh(): Promise<void>;
}
