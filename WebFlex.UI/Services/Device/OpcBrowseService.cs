using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using WebFlex.Shared.Entities.Opc;
using WebFlex.UI.DTO.Device;

namespace WebFlex.UI.Services.Device;

public class OpcBrowseService {
    public async Task<List<DeviceNodeDto>> BrowseAsync(
        OpcDevice device,
        bool onlyCollectable = true,
        CancellationToken cancellationToken = default) {
        var result = new List<DeviceNodeDto>();

        var config = await CreateConfigAsync();

#pragma warning disable CS0618
        var selectedEndpoint = CoreClientUtils.SelectEndpoint(
            config,
            device.EndpointUrl,
            device.UseSecurity
        );
#pragma warning restore CS0618

        var endpoint = new ConfiguredEndpoint(
            null,
            selectedEndpoint,
            EndpointConfiguration.Create(config)
        );

        var userIdentity = device.UseAnonymous || string.IsNullOrWhiteSpace(device.UserName)
            ? new UserIdentity(new AnonymousIdentityToken())
            : new UserIdentity(new UserNameIdentityToken {
                UserName = device.UserName,
                Password = System.Text.Encoding.UTF8.GetBytes(device.Password ?? "")
            });

#pragma warning disable CS0618
        var session = await Session.Create(
            config,
            endpoint,
            false,
            false,
            $"WebFlexBrowse-{device.DeviceCode}",
            60000,
            userIdentity,
            Array.Empty<string>()
        );
#pragma warning restore CS0618

        try {
            await BrowseNodeAsync(
                session,
                ObjectIds.ObjectsFolder,
                "",
                result,
                new HashSet<string>(),
                cancellationToken);


            if (onlyCollectable) {
                result = FilterCollectableTree(result);
            }

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

        var refs = BrowseChildReferences(session, nodeId);

        foreach (var reference in refs) {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            var childNodeId = ExpandedNodeId.ToNodeId(
                reference.NodeId,
                session.NamespaceUris);

            if (childNodeId == null) {
                continue;
            }

            // ns=0 OPC UA 표준 시스템 노드 제외
            if (childNodeId.NamespaceIndex == 0) {
                continue;
            }

            var childIdText = childNodeId.ToString();
            var browseName = reference.BrowseName.Name ?? "";

            // 언더스코어로 시작하는 KEPServer 내부 시스템 노드 제외
            if (browseName.StartsWith("_")) {
                continue;
            }

            var dto = new DeviceNodeDto {
                NodeId = childIdText,
                ParentNodeId = parentNodeId,
                DisplayName = reference.DisplayName.Text ?? "",
                BrowseName = browseName,
                NodeClass = reference.NodeClass.ToString(),
                HasChildren = reference.NodeClass == NodeClass.Object,
                DataType = "",
                IsCollectable = false
            };

            if (reference.NodeClass == NodeClass.Variable) {
                dto.DataType = ReadDataTypeText(session, childNodeId);
                // AccessLevel로 1차 필터 — 배치 Read에서 2차 필터
                dto.IsCollectable = CanCurrentRead(session, childNodeId);

                result.Add(dto);
                continue;
            }

            result.Add(dto);

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


    private static ReferenceDescriptionCollection BrowseChildReferences(
        Session session,
        NodeId nodeId) {
        var result = new ReferenceDescriptionCollection();
        var added = new HashSet<string>();

        foreach (var referenceTypeId in new[] {
            ReferenceTypeIds.Aggregates,
            ReferenceTypeIds.Organizes
        }) {
            var browser = new Browser(session) {
                BrowseDirection = BrowseDirection.Forward,
                NodeClassMask = (int)(NodeClass.Object | NodeClass.Variable),
                ReferenceTypeId = referenceTypeId,
                IncludeSubtypes = true
            };

            var refs = browser.Browse(nodeId);

            foreach (var reference in refs) {
                var key = reference.NodeId.ToString();

                if (!added.Add(key)) {
                    continue;
                }

                result.Add(reference);
            }
        }

        return result;
    }

    private static bool CanCurrentRead(
        Session session,
        NodeId nodeId) {
        try {
            var accessLevel = ReadAttribute(session, nodeId, Attributes.UserAccessLevel);

            if (accessLevel == null ||
                StatusCode.IsBad(accessLevel.StatusCode) ||
                accessLevel.Value == null) {
                accessLevel = ReadAttribute(session, nodeId, Attributes.AccessLevel);
            }

            if (accessLevel == null ||
                StatusCode.IsBad(accessLevel.StatusCode) ||
                accessLevel.Value is not byte value) {
                return false;
            }

            return (value & AccessLevels.CurrentRead) == AccessLevels.CurrentRead;
        } catch {
            return false;
        }
    }

    private static string ReadDataTypeText(
        Session session,
        NodeId nodeId) {
        try {
            var dataType = ReadAttribute(session, nodeId, Attributes.DataType);

            if (dataType == null ||
                StatusCode.IsBad(dataType.StatusCode) ||
                dataType.Value is not NodeId dataTypeNodeId) {
                return "";
            }

            return session.NodeCache.GetDisplayText(dataTypeNodeId);
        } catch {
            return "";
        }
    }

    private static DataValue? ReadAttribute(
        Session session,
        NodeId nodeId,
        uint attributeId) {
        var nodesToRead = new ReadValueIdCollection {
            new ReadValueId {
                NodeId = nodeId,
                AttributeId = attributeId
            }
        };

        session.Read(
            null,
            0,
            TimestampsToReturn.Neither,
            nodesToRead,
            out var results,
            out var diagnosticInfos);

        ClientBase.ValidateResponse(results, nodesToRead);
        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

        if (results.Count == 0) {
            return null;
        }

        return results[0];
    }

    private static List<DeviceNodeDto> FilterCollectableTree(
        List<DeviceNodeDto> nodes) {
        var map = nodes.ToDictionary(x => x.NodeId, x => x);
        var keepIds = new HashSet<string>();

        foreach (var node in nodes) {
            if (!node.IsCollectable) {
                continue;
            }

            keepIds.Add(node.NodeId);

            var parentId = node.ParentNodeId;

            while (!string.IsNullOrWhiteSpace(parentId) &&
                   map.TryGetValue(parentId, out var parent)) {
                keepIds.Add(parent.NodeId);
                parentId = parent.ParentNodeId;
            }
        }

        return nodes
            .Where(x => keepIds.Contains(x.NodeId))
            .ToList();
    }
}