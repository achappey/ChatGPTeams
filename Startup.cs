using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Bot.Connector.Authentication;
using AutoMapper;
using achappey.ChatGPTeams.Profiles;
using achappey.ChatGPTeams.Services;
using achappey.ChatGPTeams.Repositories;
using OpenAI.Managers;

//ngrok http 3978 --host-header="localhost:3978"

namespace achappey.ChatGPTeams
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appConfig = Configuration.Get<AppConfig>();

            services.AddSingleton(appConfig);
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
            services.AddSingleton<IKeyVaultRepository, KeyVaultRepository>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();

            services.AddScoped<IChatGPTeamsBotChatService, ChatGPTeamsBotChatService>();
            services.AddScoped<IChatGPTeamsBotConfigService, ChatGPTeamsBotConfigService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IResourceService, ResourceService>();
            services.AddScoped<IFunctionService, FunctionService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPromptService, PromptService>();
            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<IFunctionExecutionService, FunctionExecutionService>();
            services.AddScoped<IProactiveMessageService, ProactiveMessageService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IAssistantService, AssistantService>();
            services.AddScoped<IEmbeddingService, EmbeddingService>();
            services.AddScoped<IVaultService, VaultService>();

            services.AddScoped<ITeamsChannelMembersRepository, TeamsChannelMembersRepository>();
            services.AddScoped<ITeamsChatMembersRepository, TeamsChatMembersRepository>();
            services.AddScoped<IMembersRepository, CompositeMembersRepository>();
            services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IImageRepository, ImageRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IFunctionRepository, FunctionRepository>();
            services.AddScoped<IFilesRepository, FilesRepository>();
            services.AddScoped<IFineTuningRepository, FineTuningRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IPromptRepository, PromptRepository>();
            services.AddScoped<IRequestRepository, RequestRepository>();
            services.AddScoped<IAssistantRepository, AssistantRepository>();
            services.AddScoped<IFunctionDefinitonRepository, FunctionDefinitonRepository>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IVaultRepository, VaultRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IMessageRepository, CompositeMessageRepository>();
            services.AddScoped<ISharePointMessageRepository, SharePointMessageRepository>();
            services.AddScoped<ITeamsChatMessageRepository, TeamsChatMessageRepository>();
            services.AddScoped<ITeamsChannelMessageRepository, TeamsChannelMessageRepository>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IGraphClientFactory, GraphClientFactory>();
            services.AddHttpClient();

            services.AddMemoryCache();

            services.AddSingleton(sp => new OpenAIService(new OpenAI.OpenAiOptions()
            {
                ApiKey = appConfig.OpenAI
            }));

            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<EventBasedDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, ChatGPTeamsBot<EventBasedDialog>>();
            services.AddSingleton<IStorage, MemoryStorage>();

            // Configure AutoMapper
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfiles(new List<Profile>() {
                    new GraphProfile(),
                    new SharePointProfile(),
                    new OpenAIProfile()
                    });
            });
            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}

