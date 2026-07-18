using System.Xml.Linq;

namespace Vss.Infrastructure.Erp.SapByDesign;

/// <summary>
/// SOAP namespaces, SOAPAction values, and envelope builders for ByDesign
/// QuerySupplierIn / ManageSupplierIn. These are the common A2X shapes; the exact
/// element namespaces + write-side nesting are tenant/WSDL-specific.
/// [TODO: confirm all values in this file against the sandbox WSDL / sample payloads.]
/// </summary>
internal static class Sap
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    // Confirmed against the COJ WSDLs + a live call: the message BODY elements are in
    // SAPGlobal20/Global, while the SOAPAction uses the A1S/Global service namespace.
    private static readonly XNamespace Glob = "http://sap.com/xi/SAPGlobal20/Global";

    public const string QueryAction = "http://sap.com/xi/A1S/Global/QuerySupplierIn/FindByElementsRequest";
    public const string ManageAction = "http://sap.com/xi/A1S/Global/ManageSupplierIn/MaintainBundle_V1Request";

    // SelectionByInternalID has type SelectionByIdentifier, so its boundary element is
    // LowerBoundaryIdentifier (NOT LowerBoundaryInternalID). IntervalBoundaryTypeCode 1 = equal.
    public static string BuildQueryByInternalId(string internalId) =>
        Envelope(new XElement(Glob + "SupplierByElementsQuery_sync",
            new XElement("SupplierSelectionByElements",
                new XElement("SelectionByInternalID",
                    new XElement("InclusionExclusionCode", "I"),
                    new XElement("IntervalBoundaryTypeCode", "1"),
                    new XElement("LowerBoundaryIdentifier", internalId))),
            ProcessingConditions()));

    public static string BuildMaintainBundle(string internalId, IReadOnlyDictionary<string, string?> fields)
    {
        // Per the Manage WSDL, name fields (FirstLineName/SecondLineName) sit DIRECTLY on
        // the supplier maintain bundle — there is no Organisation wrapper. actionCode 04 = update.
        var supplier = new XElement("Supplier",
            new XAttribute("actionCode", "04"),
            new XElement("InternalID", internalId));

        if (fields.TryGetValue("LegalName", out var name) && name is not null)
            supplier.Add(new XElement("FirstLineName", name));

        // Address / contact / banking fields live under nested nodes (AddressInformation,
        // CommunicationArrangement, PurchasingData, ...) each with their own actionCode.
        // [TODO: map those against the Manage WSDL as needed — name is wired + verified.]

        return Envelope(new XElement(Glob + "SupplierBundleMaintainRequest_sync_V1",
            new XElement("BasicMessageHeader"),
            supplier));
    }

    private static XElement ProcessingConditions() =>
        new("ProcessingConditions",
            new XElement("QueryHitsMaximumNumberValue", "1"),
            new XElement("QueryHitsUnlimitedIndicator", "false"));

    private static string Envelope(XElement body) =>
        new XDocument(
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "glob", Glob.NamespaceName),
                new XElement(Soap + "Header"),
                new XElement(Soap + "Body", body)))
            .ToString(SaveOptions.DisableFormatting);
}
