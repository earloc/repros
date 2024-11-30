import nock from 'nock';

import * as v1_0_0_preview4 from '@azure/app-configuration-provider-1_0_0_preview_4';
import * as v1_1_2 from '@azure/app-configuration-provider-1_1_2';

function delay(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

export interface IOptions {
  sentinelKeyName: string,
  refreshIntervalInMs: number,
  keyFilter: string
  labelFilter: string
}

function createOptions(options: IOptions) {
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
  }
}

interface IRefreshable {
  refresh() : Promise<void>
}
type loadDelegate = (connectionString: string, options?: any) => Promise<IRefreshable>;

describe('refresh', () => {

  const baseUrl = 'http://azconfig.me.local'; //fake url

  const options = {
    keyFilter: '*',
    labelFilter: 'repro',
    refreshIntervalInMs: 1000,
    sentinelKeyName: 'sentinel'
  }

  let getSentinelRequestCount = 0;

  beforeEach( () => {
    //mock initial requests when calling 'load'
    nock(baseUrl)
      .persist()
      .get(`/kv?api-version=2023-11-01&key=*&label=${options.labelFilter}`)
      .reply(200, {
        items: [ 
          { key: 'someKey', value: 'faked', label: options.labelFilter}, 
          { key: options.sentinelKeyName, value: 'faked', label: options.labelFilter}
        ]
      }
    );

    // mock requests to the sentinel-key, which should only be made once every 'refreshIntervalInMs'
    getSentinelRequestCount = 0;
    nock(baseUrl)
      .persist()
      .get(`/kv/${options.sentinelKeyName}?api-version=2023-11-01&label=${options.labelFilter}`)
      .reply(304)
      .on('request', () => { getSentinelRequestCount++; });
  })

  test.each([
      ['1.1.2', v1_1_2.load],
      ['1.0.0-preview4', v1_0_0_preview4.load] 
    ])
    ('honors cooldown in v%p', 
    async (_, load: loadDelegate) => {
    
      const settings = await load(`Endpoint=${baseUrl};Id=id;Secret=secret`, createOptions(options));

      //during initial cooldown, refreshing should be a NOP
      for(let i = 0; i < 10; i++) {
        await settings.refresh();
      }

      // after initial cooldown, refreshing should only be done once every 'refreshIntervalInMs'
      // this was broken in 1.0.0-preview4, as this version started to spam azconfig for changes on the sentinel-keys
      // and therfore should fail for 1.0.0-preview4

      await delay(options.refreshIntervalInMs + 100);
      for(let i = 0; i < 1000; i++) {
        await settings.refresh();
      }

      expect(getSentinelRequestCount).toBe(1);

  }, 10000)
});