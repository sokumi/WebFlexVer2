using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Text;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcUaSessionFactory {
    private readonly OpcCollectorOptionState _optionState;

    public OpcUaSessionFactory(OpcCollectorOptionState optionState) {
        _optionState = optionState;
    }

    public async Task<Session> CreateSessionAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken = default) {
        var options = _optionState.Current;
        var endpointUrl = target.EndpointUrl;
        var pkiRoot = options.CertificateStoreRootPath;

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
                    StorePath = $"{pkiRoot}/own",
                    SubjectName = "WebFlexOpcCollector"
                },

                TrustedIssuerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = $"{pkiRoot}/issuer"
                },

                TrustedPeerCertificates = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = $"{pkiRoot}/trusted"
                },

                RejectedCertificateStore = new CertificateTrustList {
                    StoreType = "Directory",
                    StorePath = $"{pkiRoot}/rejected"
                },

                AutoAcceptUntrustedCertificates = options.AutoAcceptUntrustedCertificates,
                RejectSHA1SignedCertificates = options.RejectSHA1SignedCertificates,
                MinimumCertificateKeySize = (ushort)options.MinimumCertificateKeySize,
                SuppressNonceValidationErrors = options.SuppressNonceValidationErrors
            },

            TransportConfigurations = new TransportConfigurationCollection(),

            TransportQuotas = new TransportQuotas {
                OperationTimeout = options.OperationTimeoutMilliseconds,
                MaxStringLength = options.MaxStringLength,
                MaxByteStringLength = options.MaxByteStringLength,
                MaxArrayLength = options.MaxArrayLength,
                MaxMessageSize = options.MaxMessageSize,
                MaxBufferSize = options.MaxBufferSize,
                ChannelLifetime = options.ChannelLifetime,
                SecurityTokenLifetime = options.SecurityTokenLifetime
            },

            ClientConfiguration = new ClientConfiguration {
                DefaultSessionTimeout = options.DefaultSessionTimeoutMilliseconds,
                MinSubscriptionLifetime = options.MinSubscriptionLifetimeMilliseconds
            },

            DisableHiResClock = options.DisableHiResClock
        };

        Directory.CreateDirectory($"{pkiRoot}/own");
        Directory.CreateDirectory($"{pkiRoot}/issuer");
        Directory.CreateDirectory($"{pkiRoot}/trusted");
        Directory.CreateDirectory($"{pkiRoot}/rejected");

        await config.ValidateAsync(ApplicationType.Client);

        config.CertificateValidator.CertificateValidation += (sender, eventArgs) => {
            if (ServiceResult.IsGood(eventArgs.Error)) {
                eventArgs.Accept = true;
                return;
            }

            if (options.AutoAcceptUntrustedCertificates &&
                eventArgs.Error.StatusCode.Code == Opc.Ua.StatusCodes.BadCertificateUntrusted) {
                eventArgs.Accept = true;
                return;
            }

            throw new Exception(
                $"Certificate validation failed. Code={eventArgs.Error.Code}, Info={eventArgs.Error.AdditionalInfo}");
        };

#pragma warning disable CS0618
        var selectedEndpoint = CoreClientUtils.SelectEndpoint(
            config,
            endpointUrl,
            target.UseSecurity
        );
#pragma warning restore CS0618

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

#pragma warning disable CS0618
        var session = await Session.Create(
            config,
            endpoint,
            false,
            false,
            $"WebFlexOpcCollector-{target.DeviceCode}",
            (uint)options.DefaultSessionTimeoutMilliseconds,
            userIdentity,
            Array.Empty<string>()
        );
#pragma warning restore CS0618

        return session;
    }
}