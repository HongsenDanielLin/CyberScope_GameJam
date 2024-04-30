using Cinemachine;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    public CameraShake camShake;
    //������ײ���LineRenderer, Ҫ��PolygonCollider2D��LineRenderer���, 
    [SerializeField] GameObject colliderLinePrefab;

    public Vector2 mousePos;
    public float followSmooth;
    public Camera cam;

    //cam zoom
    public Camera camZoom;
    public float zoomSpeed;
    public float zoomMin;
    public float zoomMax;

    public Transform startPoint;                     //���߷������
    private float initialHight = 0;                  //���߿�ʼ����ĳ�ʼ�߶�
    public float initialVelocity = 0;                //��ʼ�ٶ�
    private float velocity_Horizontal, velocity_Vertical;  //ˮƽ���ٶȺʹ�ֱ���ٶ�
    private float includeAngle = 0;                  //��ˮƽ����ļн�
    private float totalTime = 0;                     //�׳�����ص���ʱ��
    private float timeStep = 0;                      //ʱ�䲽��

    private LineRenderer line;
    [SerializeField] private float lineWidth = 0.01f;
    [SerializeField] private Material lineMaterial;
    private RaycastHit hits;

    [Range(2, 1000)] public int line_Accuracy = 10;   //���ߵľ��ȣ��յ�ĸ���)
    private float grivaty = 9.8f;
    private int symle = 1;                           //ȷ�����µķ���
    private Vector3 parabolaPos = Vector3.zero;      //�����ߵ�����
    private Vector3 lastCheckPos, currentCheckPos;   //��һ���͵�ǰһ�����������
    private Vector3 checkPointPosition;              //����ķ�������
    private Vector3[] checkPointPos;                 //�������������
    private float timer = 0;                         //�ۼ�ʱ��
    private int lineCount = 0;
    // Start is called before the first frame update
    void Start()
    {
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
                Target_Info target = hitinfo.collider.gameObject.GetComponent<Target_Info>();
                if (target != null)
                {
                    Debug.Log("true");
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

    ////������ײline
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

    ////������ײ������
    //float colliderWidth;
    //List<Vector2> pointList2 = new List<Vector2>();
    //List<Vector2> GetColliderPath(List<Vector3> pointList3)
    //{
    //    //��ײ����
    //    colliderWidth = lineWidth;
    //    //Vector3תVector2
    //    pointList2.Clear();
    //    for (int i = 0; i < pointList3.Count; i++)
    //    {
    //        pointList2.Add(pointList3[i]);
    //    }
    //    //��ײ��������λ
    //    List<Vector2> edgePointList = new List<Vector2>();
    //    //��LineRenderer�ĵ�λΪ����, �ط��߷����뷨�߷������ƫ��һ������, �γ�һ���պ��Ҳ����������
    //    for (int j = 1; j < pointList2.Count; j++)
    //    {
    //        //��ǰ��ָ��ǰһ�������
    //        Vector2 distanceVector = pointList2[j - 1] - pointList2[j];
    //        //��������
    //        Vector3 crossVector = Vector3.Cross(distanceVector, Vector3.forward);
    //        //��׼��, ��λ����
    //        Vector2 offectVector = crossVector.normalized;
    //        //�ط��߷����뷨�߷������ƫ��һ������
    //        Vector2 up = pointList2[j - 1] + 0.5f * colliderWidth * offectVector;
    //        Vector2 down = pointList2[j - 1] - 0.5f * colliderWidth * offectVector;
    //        //�ֱ�ӵ�List����λ��ĩβ, ��֤List�еĵ�λ����Χ��һ���պ��Ҳ����������
    //        edgePointList.Insert(0, down);
    //        edgePointList.Add(up);
    //        //�������һ��
    //        if (j == pointList2.Count - 1)
    //        {
    //            up = pointList2[j] + 0.5f * colliderWidth * offectVector;
    //            down = pointList2[j] - 0.5f * colliderWidth * offectVector;
    //            edgePointList.Insert(0, down);
    //            edgePointList.Add(up);
    //        }
    //    }
    //    //���ص�λ
    //    return edgePointList;

    //}
}
