using UnityEngine;

namespace Cells.Fluid
{
    /// <summary>
    /// Experimental 2-D incompressible grid flow. Semi-Lagrangian advection,
    /// implicit viscosity, pressure projection and solid walls.
    /// Grid velocity is in Unity-units/sec; this is NOT a 3-D DNS solver.
    /// </summary>
    [DefaultExecutionOrder(-45)]
    public sealed class FluidGrid2D : MonoBehaviour
    {
        [Min(8)] public int width = 96;
        [Min(8)] public int height = 54;
        [Min(.01f)] public float spacing = .25f;
        [Min(0)] public float kinematicViscosity = .08f;
        [Range(8, 160)] public int pressureIterations = 80;
        [Range(4, 80)] public int viscosityIterations = 20;
        [Min(.001f)] public float stepSize = .02f;
        Vector2[] velocity, previous, forcing;
        float[] pressure, pressureNext, divergence;
        float accumulated;
        public bool Initialized => velocity != null && velocity.Length == width * height;
        int I(int x, int y) => y * width + x;

        void Awake() => Initialize();

        public void Initialize()
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            int n = width * height;
            velocity = new Vector2[n]; previous = new Vector2[n]; forcing = new Vector2[n];
            pressure = new float[n]; pressureNext = new float[n]; divergence = new float[n];
            accumulated = 0f;
        }

        public Vector2 Sample(Vector2 position)
        {
            if (!Initialized) Initialize();
            return Bilinear(velocity, position);
        }

        Vector2 Bilinear(Vector2[] values, Vector2 world)
        {
            Vector2 p = transform.InverseTransformPoint(world);
            float fx = Mathf.Clamp(p.x / spacing + width * .5f - .5f, 0, width - 1);
            float fy = Mathf.Clamp(p.y / spacing + height * .5f - .5f, 0, height - 1);
            int x = Mathf.Min(width - 2, Mathf.FloorToInt(fx));
            int y = Mathf.Min(height - 2, Mathf.FloorToInt(fy));
            float tx = fx - x, ty = fy - y;
            return Vector2.Lerp(Vector2.Lerp(values[I(x,y)], values[I(x+1,y)], tx),
                                Vector2.Lerp(values[I(x,y+1)], values[I(x+1,y+1)], tx), ty);
        }

        /// <summary>External body force density applied at a world position for next step.</summary>
        public void AddForce(Vector2 world, Vector2 acceleration)
        {
            if (!Initialized) Initialize();
            if (!Finite(acceleration.x) || !Finite(acceleration.y)) return;
            int index = ForceIndex(world);
            if (index >= 0) forcing[index] += acceleration;
        }

        /// <summary>
        /// Deposit a zero-net-force swimmer stresslet. Both forces must be inside the
        /// interior domain; otherwise neither is applied. A force-free swimmer cannot
        /// generate net fluid momentum merely by beating its flagellum.
        /// This is a coarse-grained approximation, not an immersed-boundary method.
        /// </summary>
        public bool AddForceDipole(Vector2 frontWorld, Vector2 rearWorld, Vector2 acceleration)
        {
            if (!Initialized) Initialize();
            if (!Finite(acceleration.x) || !Finite(acceleration.y)) return false;
            int front = ForceIndex(frontWorld);
            int rear = ForceIndex(rearWorld);
            if (front < 0 || rear < 0 || front == rear) return false;
            forcing[front] += acceleration;
            forcing[rear] -= acceleration;
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        int ForceIndex(Vector2 world)
        {
            Vector2 local = transform.InverseTransformPoint(world);
            int x = Mathf.FloorToInt(local.x / spacing + width * .5f);
            int y = Mathf.FloorToInt(local.y / spacing + height * .5f);
            return x > 0 && x < width - 1 && y > 0 && y < height - 1 ? I(x, y) : -1;
        }

        /// <summary>Sum of queued acceleration impulses before the next physics step.</summary>
        public Vector2 QueuedForceSum()
        {
            Vector2 result = Vector2.zero;
            if (forcing == null) return result;
            for (int i = 0; i < forcing.Length; i++) result += forcing[i];
            return result;
        }

        void FixedUpdate()
        {
            accumulated += Time.fixedDeltaTime;
            int count = 0;
            while (accumulated >= stepSize && count++ < 12)
            {
                Step(stepSize);
                accumulated -= stepSize;
            }
        }

        public void Step(float dt)
        {
            if (!Initialized) Initialize();
            if (!(dt > 0f) || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            for (int i=0;i<velocity.Length;i++)
            {
                velocity[i] += forcing[i] * dt;
                forcing[i] = Vector2.zero;
            }
            SolidWalls(velocity);
            System.Array.Copy(velocity, previous, velocity.Length);
            for (int y=1;y<height-1;y++)
            for (int x=1;x<width-1;x++)
            {
                int i=I(x,y);
                Vector2 world = transform.TransformPoint(new Vector3((x+.5f-width*.5f)*spacing,
                    (y+.5f-height*.5f)*spacing));
                velocity[i] = Bilinear(previous, world - previous[i]*dt);
            }
            SolidWalls(velocity);
            if (kinematicViscosity > 0f)
            {
                System.Array.Copy(velocity, previous, velocity.Length);
                float alpha = kinematicViscosity * dt / (spacing * spacing);
                for(int it=0;it<viscosityIterations;it++)
                {
                    for (int y=1;y<height-1;y++)
                    for (int x=1;x<width-1;x++)
                    {
                        int i=I(x,y);
                        velocity[i] = (previous[i] + alpha*(velocity[I(x-1,y)]+velocity[I(x+1,y)]
                            +velocity[I(x,y-1)]+velocity[I(x,y+1)]))/(1f+4f*alpha);
                    }
                    SolidWalls(velocity);
                }
            }
            Project();
        }

        void Project()
        {
            float inv2h = .5f / spacing;
            System.Array.Clear(pressure,0,pressure.Length);
            for (int y=1;y<height-1;y++)
            for (int x=1;x<width-1;x++)
                divergence[I(x,y)] = (velocity[I(x+1,y)].x-velocity[I(x-1,y)].x+
                                     velocity[I(x,y+1)].y-velocity[I(x,y-1)].y)*inv2h;
            for(int k=0;k<pressureIterations;k++)
            {
                for(int y=1;y<height-1;y++)
                for(int x=1;x<width-1;x++)
                {
                    // Reflective pressure = Neumann (zero wall-normal derivative).
                    float l = x==1 ? pressure[I(x,y)] : pressure[I(x-1,y)];
                    float r = x==width-2 ? pressure[I(x,y)] : pressure[I(x+1,y)];
                    float b = y==1 ? pressure[I(x,y)] : pressure[I(x,y-1)];
                    float t = y==height-2 ? pressure[I(x,y)] : pressure[I(x,y+1)];
                    pressureNext[I(x,y)] = (l+r+b+t-spacing*spacing*divergence[I(x,y)])*.25f;
                }
                var swap=pressure;pressure=pressureNext;pressureNext=swap;
            }
            for(int y=1;y<height-1;y++)
            for(int x=1;x<width-1;x++)
                velocity[I(x,y)] -= new Vector2((pressure[I(x+1,y)]-pressure[I(x-1,y)])*inv2h,
                                               (pressure[I(x,y+1)]-pressure[I(x,y-1)])*inv2h);
            SolidWalls(velocity);
        }

        void SolidWalls(Vector2[] v)
        {
            for(int x=0;x<width;x++) {v[I(x,0)]=Vector2.zero;v[I(x,height-1)]=Vector2.zero;}
            for(int y=0;y<height;y++) {v[I(0,y)]=Vector2.zero;v[I(width-1,y)]=Vector2.zero;}
        }

        public float MeanAbsoluteDivergence()
        {
            if(!Initialized) return 0f;
            float sum=0f;
            for(int y=2;y<height-2;y++)
            for(int x=2;x<width-2;x++)
                sum+=Mathf.Abs(velocity[I(x+1,y)].x-velocity[I(x-1,y)].x+
                               velocity[I(x,y+1)].y-velocity[I(x,y-1)].y)/(2f*spacing);
            return sum / Mathf.Max(1,(width-4)*(height-4));
        }
    }
}