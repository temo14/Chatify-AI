using ChatAI.Domain.Entities;
using ChatAI.Domain.Enums;
using ChatAI.Domain.Interfaces.Repositories;
using ChatAI.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ChatAI.Application.Features.MetaChannels.CreateConnection;

/// <summary>
/// Handler for creating Meta channel connections
/// </summary>
public class CreateMetaConnectionCommandHandler : IRequestHandler<CreateMetaConnectionCommand, CreateMetaConnectionResult>
{
    private readonly IMetaChannelConnectionRepository _connectionRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateMetaConnectionCommandHandler> _logger;
    
    public CreateMetaConnectionCommandHandler(
        IMetaChannelConnectionRepository connectionRepository,
        IEncryptionService encryptionService,
        ITenantContext tenantContext,
        ILogger<CreateMetaConnectionCommandHandler> logger)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<CreateMetaConnectionResult> Handle(CreateMetaConnectionCommand command, CancellationToken cancellationToken)
    {
        var result = new CreateMetaConnectionResult();
        
        try
        {
            _logger.LogInformation("Creating Meta {Channel} connection for tenant {TenantId}", 
                command.Channel, _tenantContext.RequiredTenantId);
            
            // Validate channel identity doesn't already exist
            var channelIdentity = GetChannelIdentity(command);
            if (!string.IsNullOrEmpty(channelIdentity))
            {
                var exists = await _connectionRepository.ChannelIdentityExistsAsync(
                    command.Channel, 
                    channelIdentity, 
                    cancellationToken);
                
                if (exists)
                {
                    result.Success = false;
                    result.ErrorMessage = $"This {command.Channel} account is already connected to another tenant.";
                    _logger.LogWarning("Duplicate channel identity detected: {Channel} {Identity}", 
                        command.Channel, channelIdentity);
                    return result;
                }
            }
            
            // Generate verify token (plain random string)
            var verifyTokenPlain = GenerateVerifyToken();
            var verifyTokenHash = _encryptionService.Hash(verifyTokenPlain);
            
            // Encrypt sensitive data
            var appSecretEncrypted = _encryptionService.Encrypt(command.MetaAppSecret);
            var accessTokenEncrypted = _encryptionService.Encrypt(command.AccessToken);
            
            // Create connection entity
            var connection = new MetaChannelConnection
            {
                TenantId = _tenantContext.RequiredTenantId,
                Channel = command.Channel,
                WebhookId = Guid.NewGuid(),
                VerifyTokenHash = verifyTokenHash,
                VerifyTokenPlain = verifyTokenPlain, // Stored temporarily for display
                MetaAppId = command.MetaAppId,
                MetaAppSecretEncrypted = appSecretEncrypted,
                AccessTokenEncrypted = accessTokenEncrypted,
                TokenKeyVersion = 1,
                FacebookPageId = command.FacebookPageId,
                InstagramBusinessAccountId = command.InstagramBusinessAccountId,
                WhatsAppPhoneNumberId = command.WhatsAppPhoneNumberId,
                WhatsAppBusinessAccountId = command.WhatsAppBusinessAccountId,
                IsActive = true
            };
            
            // Save to database
            var created = await _connectionRepository.CreateAsync(connection, cancellationToken);
            
            result.Success = true;
            result.ConnectionId = created.Id;
            result.WebhookId = created.WebhookId;
            result.VerifyToken = verifyTokenPlain;
            
            _logger.LogInformation("Created Meta connection {ConnectionId} with webhook {WebhookId}", 
                created.Id, created.WebhookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Meta connection");
            result.Success = false;
            result.ErrorMessage = "An error occurred while creating the connection.";
        }
        
        return result;
    }
    
    private string GetChannelIdentity(CreateMetaConnectionCommand command)
    {
        return command.Channel switch
        {
            MetaChannel.Messenger => command.FacebookPageId ?? string.Empty,
            MetaChannel.Instagram => command.InstagramBusinessAccountId ?? string.Empty,
            MetaChannel.WhatsApp => command.WhatsAppPhoneNumberId ?? string.Empty,
            _ => string.Empty
        };
    }
    
    private string GenerateVerifyToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, 32);
    }
}
