using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Text;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcUaSessionFactory {
    private readonly ILogger<OpcUaSessionFactory> _logger;

    public OpcUaSessionFactory(ILogger<OpcUaSessionFactory> logger) {
        _logger = logger;
    }

    public async Task<Session> CreateSessionAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken = default) {
        var endpointUrl = target.EndpointUrl;

        var config = new ApplicationConfiguration {
            ApplicationName = "WebFlexOpcCollector",
            ApplicationUri = Utils.Format(
                "urn:{0}:WebFlexOpcCollector",
                System.Net.Dns.GetHostName()),
            ProductUri = "WebFlexOpcCollector",
            ApplicationType = ApplicationType.Client,

            SecurityConfiguration = new SecurityConfiguration {
                ApplicationCertificate = new CertificateIdentifier {
                    StoreType = "Directory",
                    StorePath = "pki/own",
                    SubjectName = "WebFlexOpcCollector"
                },

                TrustedIssuerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/issuer"
                },

                TrustedPeerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/trusted"
                },

                RejectedCertificateStore = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = "pki/rejected"
                },

                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false,
                MinimumCertificateKeySize = 1024,
                SuppressNonceValidationErrors = true
            },

            TransportConfigurations = new TransportConfigurationCollection(),

            TransportQuotas = new TransportQuotas {
                OperationTimeout = 6000000,
                MaxStringLength = int.MaxValue,
                MaxByteStringLength = int.MaxValue,
                MaxArrayLength = 65535,
                MaxMessageSize = 419430400,
                MaxBufferSize = 65535,
                ChannelLifetime = -1,
                SecurityTokenLifetime = -1
            },

            ClientConfiguration = new ClientConfiguration {
                DefaultSessionTimeout = -1,
                MinSubscriptionLifetime = -1
            },

            DisableHiResClock = true
        };

        Directory.CreateDirectory("pki/own");
        Directory.CreateDirectory("pki/issuer");
        Directory.CreateDirectory("pki/trusted");
        Directory.CreateDirectory("pki/rejected");

        await config.ValidateAsync(ApplicationType.Client);

        config.CertificateValidator.CertificateValidation += (sender, eventArgs) => {
            if (ServiceResult.IsGood(eventArgs.Error)) {
                eventArgs.Accept = true;
                return;
            }

            if (eventArgs.Error.StatusCode.Code == Opc.Ua.StatusCodes.BadCertificateUntrusted) {
                eventArgs.Accept = true;
                return;
            }

            throw new Exception(
                $"Certificate validation failed. Code={eventArgs.Error.Code}, Info={eventArgs.Error.AdditionalInfo}");
        };

        var selectedEndpoint = CoreClientUtils.SelectEndpoint(
            config,
            endpointUrl,
            target.UseSecurity
        );

        _logger.LogInformation(
            "OPC Endpoint 선택 | Device={DeviceName} | RequestUrl={RequestUrl} | SelectedUrl={SelectedUrl} | SecurityMode={SecurityMode} | SecurityPolicy={SecurityPolicy}",
            target.DeviceName,
            endpointUrl,
            selectedEndpoint.EndpointUrl,
            selectedEndpoint.SecurityMode,
            selectedEndpoint.SecurityPolicyUri);

        var endpointConfiguration = EndpointConfiguration.Create(config);

        var endpoint = new ConfiguredEndpoint(
            null,
            selectedEndpoint,
            endpointConfiguration
        );

        UserIdentity userIdentity;

        if (target.UseAnonymous || string.IsNullOrWhiteSpace(target.UserName)) {
            userIdentity = new UserIdentity(new AnonymousIdentityToken());
        } else {
            userIdentity = new UserIdentity(
                new UserNameIdentityToken {
                    UserName = target.UserName,
                    Password = Encoding.UTF8.GetBytes(target.Password ?? "")
                }
            );
        }

        var session = await Session.Create(
            config,
            endpoint,
            false,
            false,
            $"WebFlexOpcCollector-{target.DeviceCode}",
            60000,
            userIdentity,
            Array.Empty<string>()
        );

        _logger.LogInformation(
            "OPC Session 생성 완료 | Device={DeviceName} | Endpoint={EndpointUrl} | Connected={Connected}",
            target.DeviceName,
            endpointUrl,
            session.Connected);

        return session;
    }
}