import nock from 'nock';

import * as v1_1_2 from '@azure/app-configuration-provider-1_1_2';
import * as v1_1_0 from '@azure/app-configuration-provider-1_1_0';
import * as v1_0_1 from '@azure/app-configuration-provider-1_0_1';
import * as v1_0_0 from '@azure/app-configuration-provider-1_0_0';
import * as v1_0_0_preview4 from '@azure/app-configuration-provider-1_0_0_preview_4';
import * as v1_0_0_preview3 from '@azure/app-configuration-provider-1_0_0_preview_3'; // refresh was introduced here, older versions incompatible from here



import { createOptions, delay, IRefreshable } from '.';

type loadDelegate = (connectionString: string, options?: any) => Promise<IRefreshable>;

describe('https://github.com/Azure/AppConfiguration-JavaScriptProvider/issues/137', () => {

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
      ['v1.1.2', v1_1_2.load],
      ['v1.1.0', v1_1_0.load],
      ['v1.0.1', v1_0_1.load],
      ['v1.0.0', v1_0_0.load],
      ['v1.0.0-preview4', v1_0_0_preview4.load],
      ['v1.0.0-preview3', v1_0_0_preview3.load],
    ])
    ('honors refresh-cooldown in %p', 
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