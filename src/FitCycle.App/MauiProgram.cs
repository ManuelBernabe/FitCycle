using Microsoft.Extensions.Logging;
using System.Net.Http;
using FitCycle.App.Services;

namespace FitCycle.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Configurar HttpClient con URL base específica por plataforma (desarrollo)
		var baseApiUrl = GetBaseApiUrl();
		builder.Services.AddSingleton<AuthenticatedHttpMessageHandler>();
		builder.Services.AddSingleton(sp =>
		{
			var handler = sp.GetRequiredService<AuthenticatedHttpMessageHandler>();
			return new HttpClient(handler)
			{
				BaseAddress = new Uri(baseApiUrl)
			};
		});

		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<IRoutineService, RoutineService>();

		return builder.Build();
	}

	private static string GetBaseApiUrl()
	{
#if ANDROID
		// Android Emulator: host machine accesible en 10.0.2.2
		return "http://10.0.2.2:5294";
#elif WINDOWS
		// Windows: usar HTTPS con dev cert de ASP.NET
		return "http://localhost:5294";
#elif IOS
		// iOS (simulador): acceder a localhost de la máquina que ejecuta la API
		return "http://localhost:5294";
#else
		return "http://localhost:5294";
#endif
	}
}
