import { AzureFunction, Context } from "@azure/functions"
import { z } from "zod";

const schema = z.object( {
    Value: z.string()
});

const setting = schema.parse(process.env);

console.warn('The setting is:', setting.Value);

const queueTrigger: AzureFunction = async function (context: Context, myQueueItem: any): Promise<void> {
    context.log('The setting is:', setting.Value);
};

export default queueTrigger;
