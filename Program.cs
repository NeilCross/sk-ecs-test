using StereoKit;

// Initialize StereoKit
SKSettings settings = new SKSettings
{
	appName      = "sk-ecs-test",
	assetsFolder = "Assets",
};
if (!SK.Initialize(settings))
	return;

// add test steppers
SK.AddStepper<Resources.ArchStepper>();
SK.AddStepper<Resources.LinqStepper>();

// add floor
Matrix   floorTransform = Matrix.TS(0, -1.5f, 0, new Vec3(30, 0.1f, 30));
Material floorMaterial  = new Material("floor.hlsl");
floorMaterial.Transparency = Transparency.Blend;

// Core application loop
SK.Run(() =>
{
	if (Device.DisplayBlend == DisplayBlend.Opaque)
		Mesh.Cube.Draw(floorMaterial, floorTransform);
});