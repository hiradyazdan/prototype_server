using prototype_ecs.Features;

namespace prototype_server.Controllers
{
    public class GameController : _BaseController
    {
        private GameFeature _feature;
        
        public GameController()
        {
        }
        
        public void Start()
        {
            _feature = new GameFeature(Contexts, Services, "Game Feature");
            
            _feature.Initialize();
        }
        
        public void FixedUpdate()
        {
            _feature.Execute();
            _feature.Cleanup();
        }
    }
}