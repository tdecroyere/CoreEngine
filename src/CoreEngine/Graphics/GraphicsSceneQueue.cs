using System;
using System.Collections.Generic;
using System.Threading;

namespace CoreEngine.Graphics
{
    public class GraphicsSceneQueue
    {
        private Queue<GraphicsScene> sceneQueue;
        
        public GraphicsSceneQueue()
        {
            this.sceneQueue = new Queue<GraphicsScene>();
        }
        // TODO: Switch to a barrier system?
        public void EnqueueScene(GraphicsScene scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            var sceneCopy = scene;//.Copy();

            lock (this.sceneQueue)
            {
                this.sceneQueue.Enqueue(sceneCopy);
            }
        }

        // Get read of the loop when the barrier system will be implemented
        public GraphicsScene WaitForNextScene()
        {
            GraphicsScene? scene;

            while (!this.sceneQueue.TryDequeue(out scene))
            {
                Thread.Sleep(100);
            }

            return scene;
        }
    }
}