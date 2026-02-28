namespace FitCycle.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("editday", typeof(Pages.EditDayPage));
		Routing.RegisterRoute("workout", typeof(Pages.WorkoutPage));
	}
}
