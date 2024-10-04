using log4net;

public static class AwsEc2MetadataReader
{
    private const string baseUrl = "http://169.254.169.254/latest";
    private const string metaUrl = $"{baseUrl}/meta-data";
    private const string tokenUrl = $"{baseUrl}/api/token";

    public const string MetadataSpotInstanceActionKey = "instance-action";
    private const string MetadataSpotInstanceActionPath = "spot/" + MetadataSpotInstanceActionKey;
    public const string MetadataSpotTerminationTimeKey = "termination-time";
    private const string MetadataSpotTerminationTimePath = "spot/" + MetadataSpotTerminationTimeKey;
    private const string MetadataInstanceLifeCycleKey = "instance-life-cycle";
    private static readonly ILog logger = LogManager.GetLogger(typeof(AwsEc2MetadataReader));
    
    public static async Task<string> GetImdsv2Token(HttpClient client)
    {
        try
        {
            client.DefaultRequestHeaders.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");
            var response = await client.PutAsync(tokenUrl, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            logger.Error("Failed to GetImdsv2Token", ex);
            return string.Empty;
        }
    }

    public static async Task<string> Ec2InstanceLifeCycleAsync()
    {
        string instanceLifeCycle = string.Empty;
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(1);
            var token = await GetImdsv2Token(client);
            if (!string.IsNullOrEmpty(token))
            {
                instanceLifeCycle = await GetMetadata(client, token, MetadataInstanceLifeCycleKey);
                if (string.IsNullOrEmpty(instanceLifeCycle))
                {
                    logger.Info($"Failed to retrieve {MetadataInstanceLifeCycleKey} from metadata.");
                }
                else
                {
                    logger.Info($"Successfully accessed metadata, {MetadataInstanceLifeCycleKey} is {instanceLifeCycle}");
                }
            }
            else
            {
                logger.Info("Failed to GetImdsv2Token for metadata.");
            }
        }
        catch (Exception ex)
        {
            logger.Info($"Failed to check EC2 metadata. Source={ex.Source} Message={ex.Message}");
        }
        return instanceLifeCycle;
    }

    public static async Task<string> GetMetadata(HttpClient client, string token, string path)
    {
        try
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-aws-ec2-metadata-token", token);
            var fullPath = path.Length > 0 ? metaUrl + "/" + path : metaUrl;
            var response = await client.GetAsync(fullPath);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : response.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to GetMetadata for {path}", ex);
            return string.Empty;
        }
    }

    private static async Task<string> GetSingleValue(HttpClient client, string token, IEnumerable<string> items, string key, string path)
    {
        var item = items.FirstOrDefault(s => s.IndexOf(key) > 0);
        return item != null ? await GetMetadata(client, token, path) : string.Empty;
    }

    public static async Task<Dictionary<string, string>> GetSpotMetadata(HttpClient client, string token)
    {
        var allDic = new Dictionary<string, string>();
        try
        {
            var latest = await GetMetadata(client, token, string.Empty);
            if (latest.Length > 0)
            {
                var rootSpotItems = latest.Split('\n').Where(s => s.StartsWith("spot"));
                if (rootSpotItems.Any())
                {
                    var latestSpot = await GetMetadata(client, token, "spot");
                    var subSpotItems = latestSpot.Split('\n').Where(s => s.IndexOf(MetadataSpotInstanceActionKey) > 0 || s.IndexOf(MetadataSpotTerminationTimeKey) > 0);
                    if (subSpotItems.Any())
                    {
                        allDic[MetadataSpotInstanceActionKey] = await GetSingleValue(client, token, subSpotItems, MetadataSpotInstanceActionKey, MetadataSpotInstanceActionPath);
                        allDic[MetadataSpotTerminationTimeKey] = await GetSingleValue(client, token, subSpotItems, MetadataSpotTerminationTimeKey, MetadataSpotTerminationTimePath);
                    }
                }
            }

            string join = string.Join("; ", allDic.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            logger.Info("Dictionary\n" + join);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to GetSpotMetadata", ex);
        }
        return allDic;
    }
}