
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using StereoKit;
using StereoKit.Framework;

namespace Resources;

record Body {
    public int Id;
    public Pose Pose;
    public Vec3 Velocity;
    public float Mass;
    public Material Mat;
}

class LinqStepper : IStepper
{
    // configuration
    const int numBodies = 100;    
    const float G = 1.81f;
    const float Drag = 0.1f;
    const int PlaySizeSq = 50;
    const float BoundsVelocityDamp = 0.6f;
    const float BodyRadius = 0.1f;
    static readonly Vec3 Centre = new Vec3(0, -0.8f,5f);
    
    // runtime data
    float lastTime = 0;
    Mesh ballMesh;
    List<Body> bodies = new List<Body>();

    public bool Enabled => true;

    public bool Initialize()
    {
        // build set of planets moving in a circle
        for (int i = 0; i < numBodies;i++)
        {
            double rad = i * Math.PI * 2 / numBodies;

            var position = Centre + new Vec3(Convert.ToSingle(Math.Sin(rad)), Convert.ToSingle(Math.Sin(rad*2)), Convert.ToSingle( Math.Cos(rad)));
            var vel = new Vec3(Convert.ToSingle(Math.Cos(rad)),0, Convert.ToSingle( -Math.Sin(rad))) * 1f;

            var minst = Material.UI.Copy();
            minst[MatParamName.ColorTint] = Color.HSV(Convert.ToSingle(Random.Shared.NextDouble()), 0.2f, 1);

            // initialise list with entities
            bodies.Add(new Body{
                Id = i,
                Pose = new Pose(position),
                Velocity = vel,
                Mass = 0.15f,
                Mat = minst
            });
        }
        ballMesh = Mesh.GenerateSphere(BodyRadius*2);
        lastTime = Time.Totalf;
        return true;
    }

    public async void Step()
    {
        var deltaTime = (Time.Totalf - lastTime);
        
        Stopwatch s = new Stopwatch();
        s.Start();
        
        var updates = bodies
            .Select((outer) => new {
                Body = outer,
                dv = bodies
                    .Except(new[]{outer})
                    .Select((inner) =>{
                        var dir = inner.Pose.position - outer.Pose.position;
                        float radius = dir.Length;
                        
                        // Fg = G * (m1 * m2) / r ^ 2
                        float force = G * (outer.Mass * inner.Mass / ((float)Math.Pow(radius,2)));
                        float acceleration = force / outer.Mass;
                        return dir.Normalized * acceleration;
                    })
                    .Aggregate(Vec3.Zero, (a, b) => a + b)
            })
            .ToList();

        updates.ForEach((update) =>
        {
            Body body = update.Body;            
            // prepare display
            if (UI.Handle($"{nameof(LinqStepper)}{body.Id}", ref body.Pose, ballMesh.Bounds))
            {
                body.Pose.position -= body.Velocity * deltaTime;
                body.Velocity = Vec3.Zero;
            }
            else
            {
                // apply calculated velocity update
                body.Velocity += update.dv * deltaTime;

                // apply drag
                // D = Cd * A * .5 * r * V^2
                // Cd is the cooeficient of drag
                // A is the reference area being acted upon
                // r is the density of the medium causing drag
                // v^2 is the relative difference in velocioty
                var fdrag = (Drag * body.Velocity.MagnitudeSq / 2) * body.Velocity.Normalized;
                body.Velocity -= fdrag * deltaTime;

                // bounce back balls when they hit the edge of the bounds
                // determine centre of display area
                var centerOffset = body.Pose.position - Centre;
                // bounce back balls when they hit the edge of the bounds
                if (centerOffset.LengthSq > PlaySizeSq) {
                    body.Velocity = -centerOffset * (1-BoundsVelocityDamp);
                }
                
                // update position
                body.Pose.position += body.Velocity * deltaTime;
            }

		    ballMesh.Draw(body.Mat, body.Pose.ToMatrix());
        });

        Console.WriteLine($"{nameof(LinqStepper)} Step took {s.Elapsed.TotalMilliseconds}ms");

        lastTime = Time.Totalf;
    }

    public void Shutdown()
    {
        // nothing to do
    }
}