

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;
using System.Collections.Generic;

public class Quote
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("quote_number")]
    public string QuoteNumber { get; set; }

    [JsonProperty("quote_date")]
    public string QuoteDate { get; set; }

    [JsonProperty("quotestatus")]
    public QuoteStatus QuoteStatus { get; set; }

    [JsonProperty("quote_subject")]
    public string QuoteSubject { get; set; }

    [JsonProperty("customer_reference")]
    public string CustomerReference { get; set; }

    [JsonProperty("sales_id")]
    public string SalesId { get; set; }

    [JsonProperty("send_type")]
    public string SendType { get; set; }

    [JsonProperty("total_excl")]
    public double TotalExcl { get; set; }
}

public class QuoteStatus
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

}
