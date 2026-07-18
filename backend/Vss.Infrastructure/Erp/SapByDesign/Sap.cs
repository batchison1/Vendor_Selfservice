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

    /// <summary>
    /// Builds a MaintainBundle_V1 update. Name (FirstLineName) sits directly on the
    /// supplier bundle. Address + email/phone live under AddressInformation, which
    /// ByDesign only accepts as a complete list (LCTI) and must carry the existing
    /// address UUID so the in-use address is updated in place, not deleted/recreated.
    /// Element order follows the WSDL schema sequence.
    /// </summary>
    public static string BuildMaintainBundle(string internalId, IReadOnlyDictionary<string, string?> fields, string? addressUuid = null)
    {
        var supplier = new XElement("Supplier",
            new XAttribute("actionCode", "04"),
            new XElement("InternalID", internalId));

        if (fields.TryGetValue("LegalName", out var name) && name is not null)
            supplier.Add(new XElement("FirstLineName", name));

        var address = new XElement("Address", new XAttribute("actionCode", "04"));
        var postal = new XElement("PostalAddress");
        void P(string field, string el)
        {
            if (fields.TryGetValue(field, out var v) && !string.IsNullOrEmpty(v)) postal.Add(new XElement(el, v));
        }
        // schema order: CountryCode, StreetName, CityName, RegionCode, StreetPostalCode
        P("RemitCountry", "CountryCode");
        P("RemitStreet", "StreetName");
        P("RemitCity", "CityName");
        P("RemitState", "RegionCode");
        P("RemitZip", "StreetPostalCode");
        if (postal.HasElements) address.Add(postal);
        // Address-level: PostalAddress, Phone, EMailURI (in that order)
        if (fields.TryGetValue("PrimaryPhone", out var phone) && !string.IsNullOrEmpty(phone))
            address.Add(new XElement("PhoneFormattedNumberDescription", phone));
        if (fields.TryGetValue("PrimaryEmail", out var email) && !string.IsNullOrEmpty(email))
            address.Add(new XElement("EMailURI", email));

        if (address.HasElements && !string.IsNullOrEmpty(addressUuid))
        {
            supplier.Add(new XAttribute("addressInformationListCompleteTransmissionIndicator", "true"));
            supplier.Add(new XElement("AddressInformation",
                new XAttribute("actionCode", "04"),
                new XElement("UUID", addressUuid),
                address));
        }

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
