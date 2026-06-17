using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.ComponentModel;
using WebFlex.Shared.Entities.Opc;
using WebFlex.UI.DTO.Device;

namespace WebFlex.UI.Services.Device;

public class OpcBrowseService {
    public async Task<List<DeviceNodeDto>> BrowseAsync(
    OpcDevice device,
    CancellationToken cancellationToken = default) {
        var result = new List<DeviceNodeDto>();

        var config = await CreateConfigAsync();

        var selectedEndpoint = CoreClientUtils.SelectEndpoint(
            config,
            device.EndpointUrl,
            device.UseSecurity);

        var endpoint = new ConfiguredEndpoint(
            null,
            selectedEndpoint,
            EndpointConfiguration.Create(config));

        var userIdentity = device.UseAnonymous || string.IsNullOrWhiteSpace(device.UserName)
            ? new UserIdentity(new AnonymousIdentityToken())
            : new UserIdentity(new UserNameIdentityToken {
                UserName = device.UserName,
                Password = System.Text.Encoding.UTF8.GetBytes(device.Password ?? "")
            });

        var session = await Session.Create(
            config,
            endpoint,
            false,
            false,
            $"WebFlexBrowse-{device.DeviceCode}",
            60000,
            userIdentity,
            Array.Empty<string>());

        try {
            await BrowseNodeAsync(
                session,
                ObjectIds.ObjectsFolder,
                "",
                result,
                new HashSet<string>(),
                cancellationToken);

            Console.WriteLine($"Browse End Count = {result.Count}");

            return result;
        } finally {
            await session.CloseAsync();
            session.Dispose();
        }
    }

    private static async Task<ApplicationConfiguration> CreateConfigAsync() {
        var config = new ApplicationConfiguration {
            ApplicationName = "WebFlexOpcBrowser",
            ApplicationUri = Utils.Format(
                "urn:{0}:WebFlexOpcBrowser",
                System.Net.Dns.GetHostName()),
            ProductUri = "WebFlexOpcBrowser",
            ApplicationType = ApplicationType.Client,

            SecurityConfiguration = new SecurityConfiguration {
                ApplicationCertificate = new CertificateIdentifier {
                    StoreType = "Directory",
                    StorePath = "pki/browser/own",
                    SubjectName = "WebFlexOpcBrowser"
                },
                TrustedIssuerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/browser/issuer"
                },
                TrustedPeerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/browser/trusted"
                },
                RejectedCertificateStore = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/browser/rejected"
                },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024,
                SuppressNonceValidationErrors = true
            },

            TransportQuotas = new TransportQuotas {
                OperationTimeout = 60000,
                MaxStringLength = int.MaxValue,
                MaxByteStringLength = int.MaxValue,
                MaxArrayLength = 65535,
                MaxMessageSize = 419430400,
                MaxBufferSize = 65535
            },

            ClientConfiguration = new ClientConfiguration {
                DefaultSessionTimeout = 60000
            }
        };

        Directory.CreateDirectory("pki/browser/own");
        Directory.CreateDirectory("pki/browser/issuer");
        Directory.CreateDirectory("pki/browser/trusted");
        Directory.CreateDirectory("pki/browser/rejected");

        await config.ValidateAsync(ApplicationType.Client);

        config.CertificateValidator.CertificateValidation += (sender, e) => {
            if (ServiceResult.IsGood(e.Error)) {
                e.Accept = true;
                return;
            }

            if (e.Error.StatusCode.Code == Opc.Ua.StatusCodes.BadCertificateUntrusted) {
                e.Accept = true;
                return;
            }

            throw new Exception($"Certificate validation failed. Code={e.Error.Code}");
        };

        return config;
    }

    private static async Task BrowseNodeAsync(
        Session session,
        NodeId nodeId,
        string parentNodeId,
        List<DeviceNodeDto> result,
        HashSet<string> visited,
        CancellationToken cancellationToken) {
        var nodeKey = nodeId.ToString();

        if (!visited.Add(nodeKey)) {
            return;
        }

        var browser = new Browser(session) {
            BrowseDirection = BrowseDirection.Forward,
            NodeClassMask = (int)(NodeClass.Object | NodeClass.Variable),
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true
        };

        var refs = browser.Browse(nodeId);

        foreach (var reference in refs) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            var childNodeId =
                ExpandedNodeId.ToNodeId(
                    reference.NodeId,
                    session.NamespaceUris);

            if (childNodeId == null) {
                continue;
            }

            var childIdText = childNodeId.ToString();

            var dto = new DeviceNodeDto {
                NodeId = childIdText,
                ParentNodeId = parentNodeId,
                DisplayName = reference.DisplayName.Text,
                BrowseName = reference.BrowseName.Name,
                NodeClass = reference.NodeClass.ToString(),
                HasChildren = reference.NodeClass == NodeClass.Object,

                // 일단 비움
                DataType = ""
            };

            result.Add(dto);

            if (result.Count % 100 == 0) {
                Console.WriteLine($"Browse Count : {result.Count}");
            }

            if (reference.NodeClass == NodeClass.Object) {
                await BrowseNodeAsync(
                    session,
                    childNodeId,
                    childIdText,
                    result,
                    visited,
                    cancellationToken);
            }
        }
    }

    private static async Task<string> ReadDataTypeAsync(Session session, NodeId nodeId) {
        try {
            var value = await session.ReadValueAsync(nodeId);
            return value.Value?.GetType().Name ?? "";
        } catch {
            return "";
        }
    }
}