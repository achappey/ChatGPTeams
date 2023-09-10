
#nullable enable

namespace achappey.ChatGPTeams.Models.Simplicate;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Invoice
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("contact_id")]
    public string? ContactId { get; set; }

    [JsonProperty("invoice_number")]
    public string? InvoiceNumber { get; set; } 

    [JsonProperty("comments")]
    public string? Comments { get; set; }

     [JsonProperty("status")]
    public InvoiceStatus Status { get; set; } = null!;

    [JsonProperty("payment_term")]
    public PaymentTerm PaymentTerm { get; set; }  = null!;

    [JsonProperty("simplicate_url")]
    public string SimplicateUrl { get; set; }  = null!;

    [JsonProperty("date")]
    public string? Date { get; set; } 

    [JsonProperty("project_id")]
    public string? ProjectId { get; set; }

    [JsonProperty("organization_id")]
    public string? OrganizationId { get; set; }

    [JsonProperty("my_organization_profile_id")]
    public string MyOrganizationProfileId { get; set; }  = null!;

    [JsonPropertyName("total_excluding_vat")]
    public decimal TotalExcludingVat { get; set; }

    [JsonPropertyName("total_outstanding")]
    public decimal TotalOutstanding { get; set; }

    [JsonProperty("subject")]
    public string? Subject { get; set; }

  //  [JsonProperty("project")]
   // public ProjectInvoice? Projects { get; set; } = null;

    [JsonProperty("organization")]
    public OrganizationInvoice? Organization { get; set; }  = null!;

}

public class InvoiceStatus
{

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }
}

public class ProjectInvoice
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class PersonInvoice
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("relation_number")]
    public string RelationNumber { get; set; }

    [JsonProperty("full_name")]
    public string FullName { get; set; }
}

public class OrganizationInvoice
{
    [JsonProperty("name")]
    public string Name { get; set; }
}

public class PaymentTerm
{
    [JsonProperty("name")]
    public string Name { get; set; }

}
