using StereoKit.Framework;
using Arch.Core;
using System;
using System.Diagnostics;
using StereoKit;
using System.Numerics;

namespace Resources;

struct Position{
    public float X;
    public float Y;
    public float Z;
    public Position(Vector3 v) {
        X = v.X;
        Y = v.Y;
        Z = v.Z;
    }
}

struct Velocity {
    public float Dx;
    public float Dy;
    public float Dz;

    public Velocity(Vector3 v) {
        Dx = v.X;
        Dy = v.Y;
        Dz = v.Z;
    }
}


struct Mass
{
    public float Weight;
}

class ArchStepper : IStepper
{
    // configuration
    const int numBodies = 100;
    const float G = 1.81f;
    const float Drag = 0.1f;
    const int PlaySizeSq = 50;    
    const float BoundsVelocityDamp = 0.6f;
    const float BodyRadius = 0.1f;
    readonly Vector3 Center = new Vector3(0, -0.8f,-5f);
    
    // runtime data
    private float lastTime = 0;    
    private Mesh ballMesh;
    private Arch.Core.World world;

    public bool Enabled => true;

    public bool Initialize()
    {
        world = Arch.Core.World.Create();
        
        // build set of planets moving in a circle
        for (int i = 0; i < numBodies;i++)
        {
            double rad = -i * Math.PI * 2 / numBodies;

            var pos = Center + new Vector3(Convert.ToSingle(Math.Sin(rad)), Convert.ToSingle(Math.Sin(rad*2)), Convert.ToSingle( Math.Cos(rad)));
            var vel = new Vector3(Convert.ToSingle(Math.Cos(rad)),0, Convert.ToSingle( -Math.Sin(rad))) * Convert.ToSingle(Random.Shared.NextDouble());

            var minst = Material.UI.Copy();
            minst[MatParamName.ColorTint] = Color.HSV(Convert.ToSingle(Random.Shared.NextDouble()), 0.7f, 1);

            // initialise world with entities
            world.Create(
                new Position(pos), 
                new Velocity(vel),
                new Mass{Weight = 0.15f},
                minst);
        }

        ballMesh = Mesh.GenerateSphere(BodyRadius*2);
        lastTime = Time.Totalf;
        return true;   
    }

    public void Step()
    { 
   
        var deltaTime = Time.Totalf - lastTime;
        
        Stopwatch s = new Stopwatch();
        s.Start();

        // accumulate gravitational force
        var query = new QueryDescription().WithAll<Position, Velocity, Mass>();
        world.Query(in query, (Entity entity, ref Position pos, ref Velocity vel, ref Mass mass) => {
            var outerPosition = pos;
            var outerMass = mass;
            world.Query(in query, (Entity other, ref Position position, ref Velocity velocity, ref Mass otherMass) => {
                if (entity != other)
                {
                    var dir = new Vector3(
                        outerPosition.X - position.X,
                        outerPosition.Y - position.Y,
                        outerPosition.Z - position.Z);
                    float radius = dir.Length();

                    // Fg = G * (m1 * m2) / r ^ 2
                    float force = G * (outerMass.Weight * otherMass.Weight) / ((float)Math.Pow(radius,2));
                    float acceleration = force / outerMass.Weight;
                    var result = Vector3.Normalize(dir) * acceleration * deltaTime;

                    // apply velocity update
                    velocity.Dx += result.X;
                    velocity.Dy += result.Y;
                    velocity.Dz += result.Z;
                }
            });
        });

        // apply position integration
        var velocityQuery = new QueryDescription().WithAll<Position, Velocity, Material>();
        world.Query(in velocityQuery, (Entity entity, ref Position position, ref Velocity vel, ref StereoKit.Material mat) => {
            
            var pose = new Pose(position.X, position.Y, position.Z);
            if (UI.Handle($"{nameof(ArchStepper)}{entity.Id}", ref pose, ballMesh.Bounds))
            {      
                position.X = pose.position.x;
                position.Y = pose.position.y;
                position.Z = pose.position.z;
                vel.Dx = 0;
                vel.Dy = 0;
                vel.Dz = 0;
            }
            else {

                var vel_vector = new Vector3(vel.Dx, vel.Dy, vel.Dz);

                // apply drag
                // D = Cd * A * .5 * r * V^2
                // Cd is the cooeficient of drag
                // A is the reference area being acted upon
                // r is the density of the medium causing drag
                // v^2 is the relative difference in velocioty
                var fdrag = (Drag * vel_vector.LengthSquared() / 2) * Vector3.Normalize(vel_vector);
                vel_vector -= fdrag  * deltaTime;

                // bounce back balls when they hit the edge of the bounds
                // determine centre of display area
                var centerOffset = pose.position.v - Center;
                // reflect the velocity back towards the center
                if (centerOffset.LengthSquared() > PlaySizeSq) {
                    vel_vector = -centerOffset * (1-BoundsVelocityDamp);
                }

                // update vel with drag and collision
                vel.Dx = vel_vector.X;
                vel.Dy = vel_vector.Y;
                vel.Dz = vel_vector.Z;
                
                // update position
                position.X += vel.Dx * deltaTime;
                position.Y += vel.Dy * deltaTime;
                position.Z += vel.Dz * deltaTime;                

                // update pose for rendering
                pose.position.x = position.X;
                pose.position.y = position.Y;
                pose.position.z = position.Z;
            }

            ballMesh.Draw(mat, pose.ToMatrix());
        });
            
        Console.WriteLine($"{nameof(ArchStepper)} Step took {s.Elapsed.TotalMilliseconds}ms");

        lastTime = Time.Totalf;
    }

    public void Shutdown()
    {
        // nothing to do
    }
}