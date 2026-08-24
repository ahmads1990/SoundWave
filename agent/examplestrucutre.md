SoundWave/
│
├── src/
│   │
│   ├── SharedKernel/                          # Zero dependencies — shared primitives only
│   │   ├── Results/
│   │   │   ├── Result.cs
│   │   │   └── Error.cs
│   │   ├── Entities/
│   │   │   └── BaseEntity.cs                  # CreatedBy, CreatedDate, IsDeleted etc
│   │   ├── Events/
│   │   │   └── IIntegrationEvent.cs           # Base interface for all outbox events
│   │   ├── Messaging/
│   │   │   └── IEventBus.cs                   # Publish abstraction used by handlers
│   │   └── Pagination/
│   │       └── PagedResult.cs
│   │
│   ├── Modules/
│   │   │
│   │   ├── Identity/
│   │   │   ├── Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── User.cs                # Aggregate root
│   │   │   │   │   └── RefreshToken.cs
│   │   │   │   ├── Errors/
│   │   │   │   │   └── IdentityErrors.cs      # Error.Unauthorized, Error.InvalidCredentials
│   │   │   │   └── Interfaces/
│   │   │   │       └── IUserRepository.cs
│   │   │   │
│   │   │   ├── Application/
│   │   │   │   ├── Abstractions/              # What Identity needs from OTHER modules
│   │   │   │   │   └── (none for Identity — it's the root module)
│   │   │   │   ├── ACL/                       # Interfaces Identity EXPOSES to other modules
│   │   │   │   │   └── IIdentityContext.cs    # UserExists, GetUserSummary, IsArtistApproved
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── Register/
│   │   │   │   │   │   ├── RegisterCommand.cs
│   │   │   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   │   │   └── RegisterCommandValidator.cs
│   │   │   │   │   ├── Login/
│   │   │   │   │   │   ├── LoginCommand.cs
│   │   │   │   │   │   ├── LoginCommandHandler.cs
│   │   │   │   │   │   └── LoginCommandValidator.cs
│   │   │   │   │   ├── RefreshToken/
│   │   │   │   │   ├── Logout/
│   │   │   │   │   ├── VerifyEmail/
│   │   │   │   │   ├── ForgotPassword/
│   │   │   │   │   └── ResetPassword/
│   │   │   │   ├── Queries/
│   │   │   │   │   └── GetUserProfile/
│   │   │   │   │       ├── GetUserProfileQuery.cs
│   │   │   │   │       ├── GetUserProfileQueryHandler.cs
│   │   │   │   │       └── UserProfileDto.cs
│   │   │   │   └── Events/                    # Integration event definitions
│   │   │   │       ├── UserRegisteredEvent.cs
│   │   │   │       └── ArtistApprovedEvent.cs
│   │   │   │
│   │   │   ├── Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── IdentityDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── UserConfiguration.cs
│   │   │   │   │   │   └── RefreshTokenConfiguration.cs
│   │   │   │   │   └── Repositories/
│   │   │   │   │       └── UserRepository.cs
│   │   │   │   ├── ACL/                       # Identity implements IIdentityContext here
│   │   │   │   │   └── IdentityContext.cs     # Used by Catalog, Playlist etc via DI
│   │   │   │   ├── Services/
│   │   │   │   │   ├── TokenService.cs        # JWT generation
│   │   │   │   │   └── PasswordService.cs     # BCrypt wrapper
│   │   │   │   ├── Consumers/                 # RabbitMQ consumers Identity owns
│   │   │   │   │   ├── SendVerificationEmailConsumer.cs   # listens: identity.user.registered
│   │   │   │   │   └── SendArtistApprovedEmailConsumer.cs # listens: identity.artist.approved
│   │   │   │   └── DependencyInjection.cs     # services.AddIdentityModule(config)
│   │   │   │
│   │   │   └── Presentation/
│   │   │       └── Endpoints/
│   │   │           ├── RegisterEndpoint.cs
│   │   │           ├── LoginEndpoint.cs
│   │   │           ├── RefreshTokenEndpoint.cs
│   │   │           └── LogoutEndpoint.cs
│   │   │
│   │   ├── Catalog/
│   │   │   ├── Domain/
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── Track.cs
│   │   │   │   │   ├── Album.cs
│   │   │   │   │   ├── Artist.cs
│   │   │   │   │   └── TrackFile.cs
│   │   │   │   ├── Enums/
│   │   │   │   │   └── TrackStatus.cs         # Pending, Processing, Ready, Failed
│   │   │   │   ├── Errors/
│   │   │   │   │   └── CatalogErrors.cs
│   │   │   │   └── Interfaces/
│   │   │   │       ├── ITrackRepository.cs
│   │   │   │       └── IFileStorage.cs        # Domain owns this abstraction
│   │   │   │
│   │   │   ├── Application/
│   │   │   │   ├── Abstractions/              # What Catalog needs FROM other modules
│   │   │   │   │   └── IIdentityContext.cs    # Catalog's copy — only what it needs
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── UploadTrack/
│   │   │   │   │   │   ├── UploadTrackCommand.cs
│   │   │   │   │   │   ├── UploadTrackCommandHandler.cs
│   │   │   │   │   │   └── UploadTrackCommandValidator.cs
│   │   │   │   │   ├── CreateAlbum/
│   │   │   │   │   ├── PublishAlbum/
│   │   │   │   │   ├── EditTrackMetadata/
│   │   │   │   │   ├── ApproveArtist/
│   │   │   │   │   └── CreateGenre/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetTrack/
│   │   │   │   │   │   ├── GetTrackQuery.cs
│   │   │   │   │   │   ├── GetTrackQueryHandler.cs
│   │   │   │   │   │   └── TrackDto.cs
│   │   │   │   │   ├── GetArtistProfile/
│   │   │   │   │   ├── GetAlbum/
│   │   │   │   │   └── GetNewReleases/
│   │   │   │   ├── Events/
│   │   │   │   │   ├── TrackUploadedEvent.cs
│   │   │   │   │   ├── TrackReadyEvent.cs
│   │   │   │   │   └── ArtistApprovedEvent.cs
│   │   │   │   └── Sagas/                     # Multi-step flows owned by Catalog
│   │   │   │       └── TrackPublishingSaga.cs  # Drives FFmpeg → Ready → ES → Qdrant → Notify
│   │   │   │
│   │   │   ├── Infrastructure/
│   │   │   │   ├── Persistence/
│   │   │   │   │   ├── CatalogDbContext.cs
│   │   │   │   │   ├── Configurations/
│   │   │   │   │   │   ├── TrackConfiguration.cs
│   │   │   │   │   │   ├── AlbumConfiguration.cs
│   │   │   │   │   │   └── TrackFileConfiguration.cs
│   │   │   │   │   ├── ReadModels/            # Thin projections of other modules' tables
│   │   │   │   │   │   └── UserReadModel.cs   # Points at Identity.Users — read only
│   │   │   │   │   └── Repositories/
│   │   │   │   │       └── TrackRepository.cs
│   │   │   │   ├── ACL/                       # Catalog's implementation of IIdentityContext
│   │   │   │   │   └── IdentityContextForCatalog.cs  # hits IdentityDbContext directly
│   │   │   │   ├── Storage/
│   │   │   │   │   └── LocalFileStorage.cs    # implements IFileStorage
│   │   │   │   ├── Processing/
│   │   │   │   │   └── FfmpegProcessor.cs     # System.Diagnostics.Process wrapper
│   │   │   │   ├── Consumers/
│   │   │   │   │   ├── TrackProcessingConsumer.cs    # listens: catalog.track.uploaded
│   │   │   │   │   ├── TrackSearchIndexConsumer.cs   # listens: catalog.track.ready (Phase 5)
│   │   │   │   │   └── ArtistIndexConsumer.cs        # listens: catalog.artist.approved (Phase 5)
│   │   │   │   └── DependencyInjection.cs
│   │   │   │
│   │   │   └── Presentation/
│   │   │       └── Endpoints/
│   │   │           ├── UploadTrackEndpoint.cs
│   │   │           ├── GetTrackEndpoint.cs
│   │   │           ├── GetArtistProfileEndpoint.cs
│   │   │           └── GetNewReleasesEndpoint.cs
│   │   │
│   │   ├── Streaming/                         # Same structure — abbreviated
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   │   ├── Abstractions/
│   │   │   │   │   ├── IIdentityContext.cs    # Streaming's slice of identity needs
│   │   │   │   │   └── ICatalogContext.cs     # IsTrackReady, GetTrackPath
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── RecordPlay/
│   │   │   │   │   ├── UpdateQueue/
│   │   │   │   │   └── SavePlaybackPosition/
│   │   │   │   └── Queries/
│   │   │   │       ├── GetStreamingUrl/
│   │   │   │       ├── GetQueue/
│   │   │   │       └── GetResumePosition/
│   │   │   ├── Infrastructure/
│   │   │   │   ├── ACL/
│   │   │   │   │   ├── IdentityContextForStreaming.cs
│   │   │   │   │   └── CatalogContextForStreaming.cs
│   │   │   │   └── Consumers/
│   │   │   │       └── PlayCountFlushWorker.cs  # BackgroundService
│   │   │   └── Presentation/
│   │   │
│   │   ├── Playlist/                          # Same pattern
│   │   │   ├── Application/
│   │   │   │   ├── Abstractions/
│   │   │   │   │   └── IIdentityContext.cs
│   │   │   │   └── Sagas/
│   │   │   │       └── CollabInviteSaga.cs    # Invite → notify → email
│   │   │   └── Infrastructure/
│   │   │       └── ACL/
│   │   │           └── IdentityContextForPlaylist.cs
│   │   │
│   │   ├── Social/
│   │   ├── Analytics/
│   │   └── Infra/                             # Cross-cutting infra concerns
│   │       ├── Domain/
│   │       ├── Application/
│   │       ├── Infrastructure/
│   │       │   ├── Messaging/
│   │       │   │   ├── RabbitMqEventBus.cs        # implements IEventBus
│   │       │   │   ├── RabbitMqTopology.cs         # ALL exchange + queue declarations
│   │       │   │   └── RabbitMqConnectionFactory.cs
│   │       │   ├── Outbox/
│   │       │   │   └── OutboxProcessorWorker.cs    # BackgroundService
│   │       │   ├── Persistence/
│   │       │   │   └── InfraDbContext.cs           # OutboxMessages, AuditLogs
│   │       │   └── DependencyInjection.cs
│   │       └── Presentation/
│   │           └── Endpoints/
│   │               └── HealthCheckEndpoint.cs
│   │
│   └── API/                                   # Entry point only — no business logic
│       ├── Program.cs                         # Wires all modules, middleware, RabbitMqTopology
│       ├── Middleware/
│       │   ├── GlobalExceptionMiddleware.cs
│       │   └── CorrelationIdMiddleware.cs
│       └── appsettings.json
│
└── tests/
    │
    ├── SharedKernel.Tests/
    │   └── Results/
    │       └── ResultTests.cs
    │
    ├── Identity.Tests/
    │   ├── Unit/
    │   │   ├── Commands/
    │   │   │   ├── RegisterCommandHandlerTests.cs
    │   │   │   ├── LoginCommandHandlerTests.cs
    │   │   │   └── RefreshTokenCommandHandlerTests.cs
    │   │   ├── Validators/
    │   │   │   ├── RegisterCommandValidatorTests.cs
    │   │   │   └── LoginCommandValidatorTests.cs
    │   │   └── ACL/
    │   │       └── IdentityContextTests.cs     # mock IdentityDbContext, test ACL queries
    │   └── Integration/
    │       └── IdentityModuleTests.cs          # Testcontainers — real MSSQL
    │
    ├── Catalog.Tests/
    │   ├── Unit/
    │   │   ├── Commands/
    │   │   │   ├── UploadTrackCommandHandlerTests.cs
    │   │   │   └── PublishAlbumCommandHandlerTests.cs
    │   │   ├── Validators/
    │   │   │   └── UploadTrackCommandValidatorTests.cs
    │   │   ├── Sagas/
    │   │   │   └── TrackPublishingSagaTests.cs  # mock consumers, verify state transitions
    │   │   └── ACL/
    │   │       └── IdentityContextForCatalogTests.cs
    │   └── Integration/
    │       ├── TrackUploadTests.cs             # full upload → outbox → status check
    │       └── FfmpegProcessorTests.cs         # real FFmpeg, real files
    │
    ├── Streaming.Tests/
    │   ├── Unit/
    │   │   ├── Commands/
    │   │   │   └── RecordPlayCommandHandlerTests.cs
    │   │   └── ACL/
    │   │       └── CatalogContextForStreamingTests.cs
    │   └── Integration/
    │       └── PlaybackSessionTests.cs
    │
    └── Architecture.Tests/                    # Enforce boundaries automatically
        └── ModuleBoundaryTests.cs