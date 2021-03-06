using RTSEngine;
using UnityEngine;

namespace TheKiwiCoder {
    public class BehaviourTreeRunner : MonoBehaviour {

        // The main behaviour tree asset
        public BehaviourTree tree;

        // Storage container object to hold game object subsystems
        Context context;

        bool initialized = false;

        bool updateAI = true;

        public void Init(GameManager gameMgr, FactionManager factionMgr)
        {
            context = CreateBehaviourTreeContext(gameMgr, factionMgr);
            tree = tree.Clone();
            tree.Bind(context);

            initialized = true;
        }

        // Update is called once per frame
        void Update() {
            //if (Input.GetKeyDown(KeyCode.P))
            //{
            //    updateAI = !updateAI;

            //    Debug.Log("Update AI: " + updateAI);
            //}

            if (initialized && tree && updateAI) {
                tree.Update();
            }
        }

        Context CreateBehaviourTreeContext(GameManager gameMgr, FactionManager factionMgr) {
            return Context.CreateFromGameObject(gameObject, gameMgr, factionMgr);
        }

        private void OnDrawGizmosSelected() {
            if (!initialized || !tree) {
                return;
            }

            BehaviourTree.Traverse(tree.rootNode, (n) => {
                if (n.drawGizmos) {
                    n.OnDrawGizmos();
                }
            });
        }
    }
}