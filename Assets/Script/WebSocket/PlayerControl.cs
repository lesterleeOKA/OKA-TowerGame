// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq; // 添加这一行

// public class NewBehaviourScript : MonoBehaviour
// {
//     public Vector3[] movePositions;
//     private Animator ani;

//     private Rigidbody2D rBody;
//     private Vector3 targetPosition;
//     private bool isMovingToTarget = false;
//     public float stoppingDistance = 0.1f; // 停止距离阈值

//     // Start is called before the first frame update
//     void Start()
//     {
//         ani = GetComponent<Animator>();
//         rBody = GetComponent<Rigidbody2D>();
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         // 处理键盘输入
//         HandleInput();

//         // 处理服务器数据同步
//         HandleServerData();

//         // 自动移动到目标位置
//         AutoMoveToTarget();
//         // float horizontal = Input.GetAxisRaw("Horizontal");

//         // float vertical = Input.GetAxisRaw("Vertical");

//         // // a/d
//         // if (horizontal != 0)
//         // {
//         //     ani.SetFloat("Horizontal", horizontal);
//         //     ani.SetFloat("Vertical", 0);
//         // }

//         // // w/s
//         // if (vertical != 0)
//         // {
//         //     ani.SetFloat("Vertical", vertical);
//         //     ani.SetFloat("Horizontal", 0);
//         // }

//         // // run
//         // Vector2 dir = new Vector2(horizontal, vertical);
//         // ani.SetFloat("Speed", dir.magnitude);

//         // // move toward direction
//         // rBody.velocity = dir * 0.5f;

//         // // for test
//         // if (WS_Client.testReceived)
//         // {
//         //     // flip player 180 degrees on Z (2D)
//         //     var euler = transform.eulerAngles;
//         //     euler.z += 180f;
//         //     transform.eulerAngles = euler;

//         //     // reset flag
//         //     WS_Client.testReceived = false;
//         // }
//     }

//     void HandleServerData()
//     {
//         if (WS_Client.gameDataReceived)
//         {
//             WS_Client.gameDataReceived = false;
//             if (WS_Client.GameData?.players != null)
//             {
//                 var myPlayer = WS_Client.GameData.players.FirstOrDefault(p => p.uid == WS_Client.Instance.pulic_UserInfo.uid);

//                 if (myPlayer != null)
//                 {
//                     // 获取服务器坐标
//                     float serverX = myPlayer.position[0];
//                     float serverY = myPlayer.position[1];
//                     float destinationX = myPlayer.destination[0];
//                     float destinationY = myPlayer.destination[1];

//                     // 立即更新当前位置到服务器同步的位置
//                     this.transform.position = new Vector3(serverX, serverY, 0f);

//                     // 设置目标位置
//                     targetPosition = new Vector3(destinationX, destinationY, 0f);
//                     isMovingToTarget = true;

//                     Debug.Log($"位置同步: 当前位置({serverX}, {serverY}) → 目标位置({destinationX}, {destinationY})");
//                 }
//             }
//         }
//     }

//     void AutoMoveToTarget()
//     {
//         if (!isMovingToTarget) return;

//         // 计算到目标的距离
//         float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

//         // 如果已经到达目标位置
//         if (distanceToTarget <= stoppingDistance)
//         {
//             isMovingToTarget = false;
//             ani.SetFloat("Speed", 0f); // 停止动画
//             // Debug.Log("已到达目标位置");
//             return;
//         }

//         // 1. 先进行移动
//         Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, 1.5f * Time.deltaTime);
//         transform.position = newPos;

//         // 2. 移动后，立即将新的位置信息更新到 WS_Client 的 _gameData 中
//         // 将 Vector3 转换为服务器需要的 float[] 格式 [x, y]
//         float[] currentPlayerPosition = new float[] { newPos.x, newPos.y };
//         // 获取当前玩家的UID
//         int currentPlayerUid = WS_Client.Instance.pulic_UserInfo.uid;

//         // 调用方法更新本地游戏数据
//         WS_Client.Instance.UpdatePlayerPositionInGameData(currentPlayerUid, currentPlayerPosition);

//         UpdateMovementAnimation(targetPosition - transform.position);
//         // Debug.Log($"移动中... 剩余距离: {distanceToTarget:F2}");
//     }

//     void UpdateMovementAnimation(Vector3 moveDirection)
//     {
//         if (moveDirection.magnitude > 0.1f)
//         {
//             // 设置动画参数
//             ani.SetFloat("Horizontal", moveDirection.x);
//             ani.SetFloat("Vertical", moveDirection.y);
//             ani.SetFloat("Speed", moveDirection.magnitude);
//         }
//     }

//     void HandleInput()
//     {
//         // 获取当前玩家的UID
//         int currentPlayerUid = WS_Client.Instance.pulic_UserInfo.uid;

//         // 原有的键盘输入处理
//         if (Input.GetKeyDown(KeyCode.W))
//         {
//             // 创建位置数据
//             WS_Client.PositionData currentPos = new WS_Client.PositionData
//             {
//                 x = this.transform.position.x,
//                 y = this.transform.position.y
//             };
//             WS_Client.PositionData destination = new WS_Client.PositionData
//             {
//                 x = this.movePositions[0].x,
//                 y = this.movePositions[0].y
//             };

//             // 更新本地_gameData中的目的地
//             float[] newPosition = new float[] { currentPos.x, currentPos.y };
//             float[] newDestination = new float[] { destination.x, destination.y };
//             WS_Client.Instance.UpdatePlayerPositionInGameData(currentPlayerUid, newPosition, newDestination);

//             // 发送位置更新到服务器
//             WS_Client.Instance.UpdateServerPosition(currentPos, destination);
//         }
//         else if (Input.GetKeyDown(KeyCode.S))
//         {
//             // 创建位置数据
//             WS_Client.PositionData currentPos = new WS_Client.PositionData
//             {
//                 x = this.transform.position.x,
//                 y = this.transform.position.y
//             };
//             WS_Client.PositionData destination = new WS_Client.PositionData
//             {
//                 x = this.movePositions[1].x,
//                 y = this.movePositions[1].y
//             };

//             // 更新本地_gameData中的目的地
//             float[] newPosition = new float[] { currentPos.x, currentPos.y };
//             float[] newDestination = new float[] { destination.x, destination.y };
//             WS_Client.Instance.UpdatePlayerPositionInGameData(currentPlayerUid, newPosition, newDestination);

//             // 发送位置更新到服务器
//             WS_Client.Instance.UpdateServerPosition(currentPos, destination);
//         }
//         else if (Input.GetKeyDown(KeyCode.A))
//         {
//             // 创建位置数据
//             WS_Client.PositionData currentPos = new WS_Client.PositionData
//             {
//                 x = this.transform.position.x,
//                 y = this.transform.position.y
//             };
//             WS_Client.PositionData destination = new WS_Client.PositionData
//             {
//                 x = this.movePositions[2].x,
//                 y = this.movePositions[2].y
//             };

//             // 更新本地_gameData中的目的地
//             float[] newPosition = new float[] { currentPos.x, currentPos.y };
//             float[] newDestination = new float[] { destination.x, destination.y };
//             WS_Client.Instance.UpdatePlayerPositionInGameData(currentPlayerUid, newPosition, newDestination);

//             // 发送位置更新到服务器
//             WS_Client.Instance.UpdateServerPosition(currentPos, destination);
//         }
//         else if (Input.GetKeyDown(KeyCode.D))
//         {
//             // 创建位置数据
//             WS_Client.PositionData currentPos = new WS_Client.PositionData
//             {
//                 x = this.transform.position.x,
//                 y = this.transform.position.y
//             };
//             WS_Client.PositionData destination = new WS_Client.PositionData
//             {
//                 x = this.movePositions[3].x,
//                 y = this.movePositions[3].y
//             };

//             // 更新本地_gameData中的目的地
//             float[] newPosition = new float[] { currentPos.x, currentPos.y };
//             float[] newDestination = new float[] { destination.x, destination.y };
//             WS_Client.Instance.UpdatePlayerPositionInGameData(currentPlayerUid, newPosition, newDestination);

//             // 发送位置更新到服务器
//             WS_Client.Instance.UpdateServerPosition(currentPos, destination);
//         }
//     }
// }
