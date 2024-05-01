using Cinemachine;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    private GameManager gameManager;
    public CameraShake camShake;
    //带有碰撞体的LineRenderer, 要有PolygonCollider2D、LineRenderer组件, 
    [SerializeField] GameObject colliderLinePrefab;

    public Vector2 mousePos;
    public float followSmooth;
    public Camera cam;

    //cam zoom
    public Camera camZoom;
    public float zoomSpeed;
    public float zoomMin;
    public float zoomMax;

    public Transform startPoint;                     //射线发射起点
    private float initialHight = 0;                  //射线开始发射的初始高度
    public float initialVelocity = 0;                //初始速度
    private float velocity_Horizontal, velocity_Vertical;  //水平分速度和垂直分速度
    private float includeAngle = 0;                  //与水平方向的夹角
    private float totalTime = 0;                     //抛出到落地的总时间
    private float timeStep = 0;                      //时间步长

    private LineRenderer line;
    [SerializeField] private float lineWidth = 0.01f;
    [SerializeField] private Material lineMaterial;
    private RaycastHit hits;

    [Range(2, 1000)] public int line_Accuracy = 10;   //射线的精度（拐点的个数)
    private float grivaty = 9.8f;
    private int symle = 1;                           //确定上下的符合
    private Vector3 parabolaPos = Vector3.zero;      //抛物线的坐标
    private Vector3 lastCheckPos, currentCheckPos;   //上一个和当前一个监测点的坐标
    private Vector3 checkPointPosition;              //监测点的方向向量
    private Vector3[] checkPointPos;                 //监测点的坐标数组
    private float timer = 0;                         //累计时间
    private int lineCount = 0;
    // Start is called before the first frame update

    //ShootEffect
    public GameObject windowBrokenVFX;
    public GameObject targetBrokenVFX;
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        //cam = FindObjectOfType<Camera>();
        zoomMax = camZoom.orthographicSize;
        if (!this.GetComponent<LineRenderer>())
        {
            line = this.gameObject.AddComponent<LineRenderer>();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = lineMaterial;
        }
        camShake = FindObjectOfType<CameraShake>();
    }

    // Update is called once per frame
    void Update()
    {
        if (startPoint == null)
        {
            return;
        }
        //Shooting();
        mousePos = new Vector2(cam.ScreenPointToRay(Input.mousePosition).origin.x, cam.ScreenPointToRay(Input.mousePosition).origin.y);

        MouseFollow();
        Shooting();
        ScopeScroll();
    }

    public void MouseFollow()
    {
        if (Vector2.Distance(startPoint.transform.position, mousePos) > 0.1f)
        {
            startPoint.transform.position = Vector2.Lerp(startPoint.transform.position,mousePos, Time.deltaTime*followSmooth);
        }
    }
    public void Shooting()
    {
        if (Input.GetMouseButtonDown(0))
        {
            camShake.PlayerShakeAnimation();
            //camShake.ShakeCamera();
            Calculation_parabola();
        }
    }

    public void ScopeScroll()
    {
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");
        if (scrollValue != 0)
        {         
            camZoom.orthographicSize += scrollValue * Time.deltaTime * zoomSpeed;           
        }

            if (camZoom.orthographicSize < zoomMin)
            {
            camZoom.orthographicSize = zoomMin;
            }                      
        

            if (camZoom.orthographicSize > zoomMax)
            {
            camZoom.orthographicSize = zoomMax;
            }
        
    }

    private void Calculation_parabola()
    {
        velocity_Horizontal = initialVelocity * Mathf.Cos(includeAngle);
        velocity_Vertical = initialVelocity * Mathf.Sin(includeAngle);
        initialHight = Mathf.Abs(startPoint.transform.position.y);
        float time_1 = velocity_Vertical / grivaty;
        float time_2 = Mathf.Sqrt((time_1 * time_1) + (2 * initialHight) / grivaty);
        totalTime = time_1 + time_2;
        timeStep = totalTime / line_Accuracy;
        includeAngle = Vector3.Angle(startPoint.forward, Vector3.ProjectOnPlane(startPoint.forward, Vector3.up)) * Mathf.Deg2Rad;
        symle = (startPoint.position + startPoint.forward).y > startPoint.position.y ? 1 : -1;

        if (checkPointPos == null || checkPointPos.Length != line_Accuracy)
        {
            checkPointPos = new Vector3[line_Accuracy];
        }
        for (int i = 0; i < line_Accuracy; i++)
        {
            if (i == 0)
            {
                lastCheckPos = startPoint.position - startPoint.forward;
            }
            parabolaPos.z = velocity_Horizontal * timer;
            parabolaPos.y = velocity_Vertical * timer * symle + (-grivaty * timer * timer) / 2;
            currentCheckPos = startPoint.position + Quaternion.AngleAxis(startPoint.eulerAngles.y, Vector3.up) * parabolaPos;
            checkPointPosition = currentCheckPos - lastCheckPos;
            lineCount = i + 1;
            if (Physics.Raycast(lastCheckPos, checkPointPosition, out hits, checkPointPosition.magnitude + 3))
            {
                checkPointPosition = hits.point - lastCheckPos;
                checkPointPos[i] = hits.point;

                //point.SetActive(true);
                //point.transform.position = hits.point;
                //point.transform.localScale = Vector3.one / 3;
                //point.transform.GetComponent<MeshRenderer>().material.color = Color.red;
                //if (hits.transform == null)
                //{
                //    point.SetActive(false);

                //}
            }
            
            Ray collideRay = new Ray(lastCheckPos, checkPointPosition * 1);
            
            Debug.DrawRay(lastCheckPos, checkPointPosition*1, Color.blue, 1f);
            //Debug.DrawLine(lastCheckPos, currentCheckPos,Color.red,1f);
            
            bool isCollide = Physics.Raycast(collideRay, out RaycastHit hitinfo,1);
            if(isCollide)
            {
                WindowCollider windows = hitinfo.collider.gameObject.GetComponent<WindowCollider>();
                TargetCollider targetCol = hitinfo.collider.gameObject.GetComponent<TargetCollider>();
                if (windows != null)
                {
                    Instantiate(windowBrokenVFX, hitinfo.point, Quaternion.identity);
                }

                if (targetCol != null)
                {
                    Debug.Log("collide");
                   GameObject targetColVFX = Instantiate(targetBrokenVFX, hitinfo.transform);
                    targetColVFX.transform.position = hitinfo.point;
                }
                Target_Info target = hitinfo.collider.gameObject.GetComponent<Target_Info>();
                if (target != null)
                {
                    Debug.Log(target.score);
                    target.Shoot();
                    Destroy(target.gameObject);
                    break;
                }
                
            }

            checkPointPos[i] = currentCheckPos;
            lastCheckPos = currentCheckPos;
            timer += timeStep;
        }
        line.positionCount = lineCount;
        line.SetPositions(checkPointPos);
        timer = 0;
    }

    ////生成碰撞line
    //void CreateColliderLine(List<Vector3> pointList)
    //{
    //    GameObject prefab = Instantiate(colliderLinePrefab, transform);
    //    LineRenderer lineRenderer = prefab.GetComponent<LineRenderer>();
    //    PolygonCollider2D polygonCollider = prefab.GetComponent<PolygonCollider2D>();

    //    lineRenderer.positionCount = pointList.Count;
    //    lineRenderer.SetPositions(pointList.ToArray());

    //    List<Vector2> colliderPath = GetColliderPath(pointList);
    //    polygonCollider.SetPath(0, colliderPath.ToArray());
    //}

    ////计算碰撞体轮廓
    //float colliderWidth;
    //List<Vector2> pointList2 = new List<Vector2>();
    //List<Vector2> GetColliderPath(List<Vector3> pointList3)
    //{
    //    //碰撞体宽度
    //    colliderWidth = lineWidth;
    //    //Vector3转Vector2
    //    pointList2.Clear();
    //    for (int i = 0; i < pointList3.Count; i++)
    //    {
    //        pointList2.Add(pointList3[i]);
    //    }
    //    //碰撞体轮廓点位
    //    List<Vector2> edgePointList = new List<Vector2>();
    //    //以LineRenderer的点位为中心, 沿法线方向与法线反方向各偏移一定距离, 形成一个闭合且不交叉的折线
    //    for (int j = 1; j < pointList2.Count; j++)
    //    {
    //        //当前点指向前一点的向量
    //        Vector2 distanceVector = pointList2[j - 1] - pointList2[j];
    //        //法线向量
    //        Vector3 crossVector = Vector3.Cross(distanceVector, Vector3.forward);
    //        //标准化, 单位向量
    //        Vector2 offectVector = crossVector.normalized;
    //        //沿法线方向与法线反方向各偏移一定距离
    //        Vector2 up = pointList2[j - 1] + 0.5f * colliderWidth * offectVector;
    //        Vector2 down = pointList2[j - 1] - 0.5f * colliderWidth * offectVector;
    //        //分别加到List的首位和末尾, 保证List中的点位可以围成一个闭合且不交叉的折线
    //        edgePointList.Insert(0, down);
    //        edgePointList.Add(up);
    //        //加入最后一点
    //        if (j == pointList2.Count - 1)
    //        {
    //            up = pointList2[j] + 0.5f * colliderWidth * offectVector;
    //            down = pointList2[j] - 0.5f * colliderWidth * offectVector;
    //            edgePointList.Insert(0, down);
    //            edgePointList.Add(up);
    //        }
    //    }
    //    //返回点位
    //    return edgePointList;

    //}
}
