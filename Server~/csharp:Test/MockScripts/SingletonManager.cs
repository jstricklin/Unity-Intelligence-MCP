// public class SingletonManager : MonoBehaviour
// {
//     private static SingletonManager _instance;
    
//     public static SingletonManager Instance {
//         get { return _instance; }
//     }
    
//     private void Awake() 
//     {
//         if (_instance != null && _instance != this) 
//         { 
//             Destroy(this.gameObject);
//             return;
//         }
        
//         _instance = this;
//         DontDestroyOnLoad(this.gameObject);
//     }
// }
