using Godot;

public partial class HelloWorld : Node2D
{
    public override void _Ready()
    {
        GD.Print("Hello World from C#!");
        GD.Print($"Godot {Engine.GetVersionInfo()["string"]}");
        GD.Print($".NET {System.Environment.Version}");
        GD.Print("Pipeline verified — ready to build.");

        // Auto-quit in headless mode (CLI testing)
        if (DisplayServer.GetName() == "headless")
        {
            GetTree().Quit();
        }
    }
}
