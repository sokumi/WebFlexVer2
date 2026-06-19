using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Text;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcUaSessionFactory {
    private readonly OpcClientOptionState _optionState;

    public OpcUaSessionFactory(OpcClientOptionState optionState) {
        _optionState = optionState;
    }

    public async Task<Session> CreateSessionAsync(
        OpcCollectTargetDto target,
        CancellationToken cancellationToken = default) {
        var options = _optionState.Current;
        var endpointUrl = target.EndpointUrl;

        var config = new ApplicationConfiguration {
            ApplicationName = options.ApplicationName,
            ApplicationUri = options.ApplicationUri,
            ProductUri = options.ProductUri,
            ApplicationType = ApplicationType.Client,

            SecurityConfiguration = new SecurityConfiguration {
                ApplicationCertificate = new CertificateIdentifier {
                    StoreType = options.ApplicationCertificateStoreType,
                    StorePath = options.ApplicationCertificateStorePath,
                    SubjectName = options.ApplicationCertificateSubjectName,
                    Thumbprint = string.IsNullOrWhiteSpace(options.ApplicationCertificateThumbprint)
                        ? null
                        : options.ApplicationCertificateThumbprint
                },

                TrustedIssuerCertificates = new CertificateTrustList {
                    StoreType = options.TrustedIssuerCertificatesStoreType,
                    StorePath = options.TrustedIssuerCertificatesStorePath
                },

                TrustedPeerCertificates = new CertificateTrustList {
                    StoreType = options.TrustedPeerCertificatesStoreType,
                    StorePath = options.TrustedPeerCertificatesStorePath
                },

                RejectedCertificateStore = new CertificateTrustList {
                    StoreType = options.RejectedCertificateStoreType,
                    StorePath = options.RejectedCertificateStorePath
                },

                AutoAcceptUntrustedCertificates = options.AutoAcceptUntrustedCertificates,
                RejectSHA1SignedCertificates = options.RejectSHA1SignedCertificates,
                RejectUnknownRevocationStatus = options.RejectUnknownRevocationStatus,
                MinimumCertificateKeySize = (ushort)options.MinimumCertificateKeySize,
                AddAppCertToTrustedStore = options.AddAppCertToTrustedStore,
                SuppressNonceValidationErrors = options.SuppressNonceValidationErrors,
                SendCertificateChain = options.SendCertificateChain
            },

            TransportConfigurations = new TransportConfigurationCollection(),

            TransportQuotas = new TransportQuotas {
                OperationTimeout = options.OperationTimeout,
                MaxStringLength = options.MaxStringLength,
                MaxByteStringLength = options.MaxByteStringLength,
                MaxArrayLength = options.MaxArrayLength,
                MaxMessageSize = options.MaxMessageSize,
                MaxBufferSize = options.MaxBufferSize,
                ChannelLifetime = options.ChannelLifetime,
                SecurityTokenLifetime = options.SecurityTokenLifetime
            },

            ClientConfiguration = new ClientConfiguration {
                DefaultSessionTimeout = options.DefaultSessionTimeout,
                MinSubscriptionLifetime = options.MinSubscriptionLifetime
            },

            DisableHiResClock = options.DisableHiResClock
        };

        CreateCertificateDirectories(options);

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
            target.UseSecurity || options.UseSecurity
        );
#pragma warning restore CS0618

        var endpointConfiguration = EndpointConfiguration.Create(config);

        var endpoint = new ConfiguredEndpoint(
            null,
            selectedEndpoint,
            endpointConfiguration
        );

        UserIdentity userIdentity;

        var useAnonymous =
            target.UseAnonymous ||
            options.IdentityType.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);

        if (useAnonymous || string.IsNullOrWhiteSpace(target.UserName)) {
            userIdentity = new UserIdentity(new AnonymousIdentityToken());
        } else {
            userIdentity = new UserIdentity(
                new UserNameIdentityToken {
                    UserName = string.IsNullOrWhiteSpace(target.UserName) ? options.UserName : target.UserName,
                    Password = Encoding.UTF8.GetBytes(
                        string.IsNullOrWhiteSpace(target.Password) ? options.Password : target.Password)
                }
            );
        }

        var preferredLocales = ParseCsv(options.PreferredLocales);

#pragma warning disable CS0618
        var session = await Session.Create(
            config,
            endpoint,
            options.UpdateBeforeConnect,
            options.CheckDomain,
            string.IsNullOrWhiteSpace(options.SessionName)
                ? $"WebFlexOpcCollector-{target.DeviceCode}"
                : options.SessionName,
            (uint)Math.Max(1, options.SessionTimeout),
            userIdentity,
            preferredLocales
        );
#pragma warning restore CS0618

        return session;
    }

    private static void CreateCertificateDirectories(OpcClientOptionDto options) {
        CreateDirectoryIfDirectoryStore(options.ApplicationCertificateStoreType, options.ApplicationCertificateStorePath);
        CreateDirectoryIfDirectoryStore(options.TrustedIssuerCertificatesStoreType, options.TrustedIssuerCertificatesStorePath);
        CreateDirectoryIfDirectoryStore(options.TrustedPeerCertificatesStoreType, options.TrustedPeerCertificatesStorePath);
        CreateDirectoryIfDirectoryStore(options.RejectedCertificateStoreType, options.RejectedCertificateStorePath);
    }

    private static void CreateDirectoryIfDirectoryStore(string storeType, string storePath) {
        if (!storeType.Equals("Directory", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(storePath))
            return;

        Directory.CreateDirectory(storePath);
    }

    private static string[] ParseCsv(string value) {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}