using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.ThirdPerson;
using System.IO;
using UMA.CharacterSystem;
using System;
using Normal.Realtime;


[RequireComponent(typeof(ThirdPersonCharacter))]
public class ThirdPersonUserControl : MultiplayerMonoBehavior
{

    public RealtimeView _realtimeView;
    private RealtimeTransform _realtimeTransform;

    private float offsetx = 0f;
    private float offsety = 1.25f;
    private float offsetz = -2f;

    private ThirdPersonCharacter m_Character; // A reference to the ThirdPersonCharacter on the object
    private Transform m_Cam;                  // A reference to the main camera in the scenes transform
    private Vector3 m_CamForward;             // The current forward direction of the camera
    private Vector3 m_Move;
    private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.

    GameObject m_MainCamera;
    EventManager eventManager;
    public Transform playerCamera;
    private ThirdPersonOrbitCamBasic camScript;

    ModifyInteraction interactionModifier;

    public DynamicCharacterAvatar avatar;
    public string avatarRecipe;
    public string foreignAvatarRecipe;

    public bool sit = false;
    public Vector3 positionBeforeSitting;

    private bool isCameraParented = false;

    private int currentNumPlayers;
    private int prevNumPlayers;

    //Camera focus stuff
    private bool canMove = true;
    private bool autoPilot = false;
    private Vector3 autoTarget;
    private Vector3 rotateTowardsTarget;

    private GameObject FocusCameraPosition;
    private Transform cameraStay;
    private string prevFocusState;

    private string currentInteraction;

    private int numberOfAnimations;

    public GameObject diplomaPrefab;
    private GameObject diploma;
    public GameObject diplomaUI;

    private GameObject rightHand;

    
    private void Awake()
    {
        _realtimeView = GetComponent<RealtimeView>();
        _realtimeTransform = GetComponent<RealtimeTransform>();
    }

    public bool isLocallyOwned()
    {
        return _realtimeView.isOwnedLocally;
    }

    public int getID()
    {
        return _realtimeView.ownerID;
    }
    public void GetDiploma(){
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        GetComponent<StateSync>().SetState("0_1_0_"+cur_time.ToString());
    }

    public void GiveDiploma(){
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        GetComponent<StateSync>().SetState("0_0_0_"+cur_time.ToString());
    }

    private GameObject rosette;

     public GameObject GetChildWithName(GameObject fromGameObject, string withName)
    {
        Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }

    private GameObject chest;

    public void GetPinned(){
        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        GetComponent<StateSync>().SetState("0_2_0_"+cur_time.ToString());
    }
   

    private bool firstAnimationOfInitial = true;
    private bool firstAnimationOfMiddle = true;
    
    public void DiplomaEventInitial(int i){

        if(i==0){
            if(firstAnimationOfInitial){
                GetDiploma(); 
                firstAnimationOfInitial = false;
                return;
            }
            if(!firstAnimationOfInitial){
                firstAnimationOfInitial = true;
            }
        }
        if(i==1){
            if(firstAnimationOfInitial){
                GiveDiploma();  
                firstAnimationOfInitial = false;
                return;
            }
            if(!firstAnimationOfInitial){
                GiveDiploma();
                GetPinned();
                firstAnimationOfInitial = true;
            }
        }
    }
    public void DiplomaEventMiddle(int i){

       if(i==0){
            if(firstAnimationOfMiddle){
                GiveDiploma();
                firstAnimationOfMiddle = false;
                return;
            }
            if(!firstAnimationOfMiddle){
                firstAnimationOfMiddle = true;
            }
        }
        else{
            if(firstAnimationOfMiddle){
                // GiveDiploma(); 
                firstAnimationOfMiddle = false;
                return;
            }
            if(!firstAnimationOfMiddle){
                GetDiploma();
                firstAnimationOfMiddle = true;
            }
        }
    }

    private void Start()
    {
        // get the transform of the main camera
        if (Camera.main != null)
        {
            m_Cam = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning(
                "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
            // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
        }

        // get the third person character ( this should never be null due to require component )
        m_Character = GetComponent<ThirdPersonCharacter>();
        eventManager = GameObject.Find("EventManager").GetComponent<EventManager>();
        eventManager.OnEventsChange.AddListener(SwitchFocus);

        interactionModifier = GetComponent<ModifyInteraction>();


        RecipeSync _recipeSync;
        _recipeSync = GetComponent<RecipeSync>();

        if (_realtimeView.isOwnedLocally)
        {
            ActionRouter.SetLocalAvatar(transform.gameObject);
            _recipeSync.SetRecipe(PlayerPrefs.GetString("playerRecipe"));

        }
        else
        {
            transform.gameObject.layer = LayerMask.NameToLayer("RemoteAvatar");
        }

        avatar.ClearSlots();
        avatar.LoadFromRecipeString(_recipeSync.GetRecipe());

        gameObject.name = "Avatar_" + getID();
        numberOfAnimations = Utils.animations.Length;
    }

    private string[] stringToArray(string s)
    {
        string[] parameters;

        //"(float forwardamount) (float turnamount) (int crouching) (int onGround)"
        parameters = s.Split(' ');
        return parameters;
    }

    public void ReceivedRemoteAction(string lastAction)
    {

        if (_realtimeView.isOwnedLocally)
        {
            // This method only applies to remote avatars
            return;
        }

        string[] actionParts = lastAction.Split('_');

        m_Character.StartAnimation(actionParts[0], isAnimationLocal);

    }

    private bool isAnimationLocal;
    public void ReactToInteractionChange(GameObject sourceCharacter, string newInteraction)
    {
        LOG("Interaction type: " + newInteraction);
        LOG("ReactToInteractionchange from " + gameObject.name);

        string[] parameters = stringToArray(newInteraction);

        isAnimationLocal = false;
        if(Convert.ToInt32(parameters[0]) == _realtimeView.ownerID){
            isAnimationLocal = true;
        }

        currentInteraction = parameters[2];


        float[] animationReq = Utils.animationRequirements[Convert.ToInt32(parameters[2])];

        if (_realtimeView.isOwnedLocally)
        {

            Vector3 otherPosition = sourceCharacter.transform.position;
            Vector3 diff = (otherPosition - transform.position);

            Vector3 centerTarget = (diff / 2) + transform.position;

            Vector3 centerToTargetVect = (transform.position - centerTarget);
            centerToTargetVect.Normalize();

            Vector3 dirVector = Quaternion.AngleAxis(90, Vector3.up) * centerToTargetVect;

            Vector3 moveToTarget = centerTarget + centerToTargetVect * (animationReq[0] / 2);
            Vector3 lookAtTarget = centerTarget + dirVector * animationReq[1];

            canMove = false;
            autoPilot = true;
            autoTarget = moveToTarget;
            rotateTowardsTarget = lookAtTarget;
        }


    }

    int maxId = -1;

    public void ReactToPodiumChange(int newPodium)
    {

        if (!_realtimeView.isOwnedLocally) { return; }

        if (newPodium == -1)
        {
            Debug.Log("Podium reset");
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (maxId != -1)
                    if (player.GetComponent<ThirdPersonUserControl>().getID() == maxId)
                    {
                        player.GetComponent<AudioSource>().spatialBlend = 1;
                    }
            }
            return;
        }

        Debug.Log("podium changed to " + newPodium.ToString());

        float maxDistance = 1000;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            float dist = (player.transform.position - GameObject.Find("Podium").transform.position).magnitude;

            if (dist < maxDistance)
            {
                maxDistance = dist;
                maxId = player.GetComponent<ThirdPersonUserControl>().getID();
            }
        }

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (maxId != -1)
                if (player.GetComponent<ThirdPersonUserControl>().getID() == maxId)
                {
                    player.GetComponent<AudioSource>().spatialBlend = 0;
                }
        }

    }

    private void SwitchFocus()
    {
        if (!_realtimeView.isOwnedLocally)
            return;

        FocusCameraPosition = GameObject.Find("FocusCamPos");

        if (eventManager.GetEvents()[2] == '1')
        {
            cameraStay = Camera.main.transform;
            camScript.AssignPlayer(FocusCameraPosition.transform);
            Camera.main.GetComponent<SimpleCameraController>().enabled = true;
            //Camera.main.GetComponent<ThirdPersonUserControl>().enabled = true;
            canMove = false;
            GameObject.Find("Realtime").GetComponent<AdminPanel>().FocusCamera();
        }
        else if (prevFocusState == "1" && eventManager.GetEvents()[2] == '0')
        {
            Camera.main.transform.position = cameraStay.position;
            Camera.main.transform.rotation = cameraStay.rotation;
            camScript.AssignPlayer(transform);
            Camera.main.GetComponent<SimpleCameraController>().enabled = false;
            //Camera.main.GetComponent<ThirdPersonUserControl>().enabled = true;
            canMove = true;
            GameObject.Find("Realtime").GetComponent<AdminPanel>().UnfocusCamera();
        }

        prevFocusState = eventManager.GetEvents()[2].ToString();
    }


    private void Update()
    {
        // If this CubePlayer prefab is not owned by this client, bail.
        if (!_realtimeView.isOwnedLocally)
            return;

        // Move the camera
        if (!isCameraParented)
        {
            m_MainCamera = Camera.main.gameObject;
            m_MainCamera.transform.parent = transform;
            Vector3 offset = new Vector3(offsetx, offsety, offsetz);
            m_MainCamera.transform.position = transform.position + offset;
            m_MainCamera.transform.LookAt(transform);

            playerCamera = m_MainCamera.transform;

            camScript = playerCamera.GetComponent<ThirdPersonOrbitCamBasic>();
            camScript.AssignPlayer(transform);

            isCameraParented = true;
        }

        // Make sure we own the transform so that RealtimeTransform knows to use this client's transform to synchronize remote clients.
        _realtimeTransform.RequestOwnership();

        if (canMove)
        {
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

        }
    }

    private string SerializeMove(Vector3 move, bool[] animationStates)
    {
        string animationString = move.x.ToString() + " " + move.y.ToString() + " " + move.z.ToString() + " ";
        foreach (bool animation in animationStates)
        {
            animationString += Convert.ToInt16(animation) + " ";
        }
        return animationString;
    }


    // Fixed update is called in sync with physics
    private void FixedUpdate()
    {
        // If this CubePlayer prefab is not owned by this client, bail.
        if (!_realtimeView.isOwnedLocally)
        {

            m_Character.ForeignMove(GetComponent<UpdateMove>().GetCharacterMove());

        }
        else
        {

            // Move the camera
            if (!isCameraParented)
            {
                m_MainCamera = Camera.main.gameObject;
                m_MainCamera.transform.parent = transform;
                Vector3 offset = new Vector3(offsetx, offsety, offsetz);
                m_MainCamera.transform.position = transform.position + offset + new Vector3(0, -1, 0);
                m_MainCamera.transform.LookAt(transform);

                playerCamera = m_MainCamera.transform;

                camScript = playerCamera.GetComponent<ThirdPersonOrbitCamBasic>();
                camScript.AssignPlayer(transform);

                isCameraParented = true;
            }

            // Make sure we own the transform so that RealtimeTransform knows to use this client's transform to synchronize remote clients.
            _realtimeTransform.RequestOwnership();

            if (canMove)
            {
                // read inputs
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");

                if ((h != 0f || v != 0f) && sit)
                {
                    Debug.Log("Stand up");
                    sit = false;
                    transform.position = positionBeforeSitting;
                    //GetComponent<CapsuleCollider>().enabled = true;
                    Physics.IgnoreCollision(ActionRouter.GetCurrentChair().GetComponent<Collider>(), GetComponent<Collider>(), false);
                    //GetComponent<Rigidbody>().useGravity = true;

                }

                if (sit)
                {
                    //GetComponent<CapsuleCollider>().enabled = false;
                    Physics.IgnoreCollision(ActionRouter.GetCurrentChair().GetComponent<Collider>(), GetComponent<Collider>());
                    //GetComponent<Rigidbody>().useGravity = false;
                }

                bool[] animationStates = new bool[numberOfAnimations];

                animationStates[0] = m_Jump;
                animationStates[1] = sit;

                for (int i = 2; i < numberOfAnimations; i += 1)
                {
                    animationStates[i] = Input.GetKey(Utils.animationEnums[i - 2]);
                }


                // calculate move direction to pass to character
                if (m_Cam != null)
                {
                    // calculate camera relative direction to move:
                    m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                    m_Move = v * m_CamForward + h * m_Cam.right;
                }
                else
                {
                    // we use world-relative directions in the case of no main camera
                    m_Move = v * Vector3.forward + h * Vector3.right;
                }
#if !MOBILE_INPUT
                // walk speed multiplier
                if (!Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

                // pass all parameters to the character control script
                m_Character.Move(m_Move, animationStates);


                GetComponent<UpdateMove>().UpdateCharacterMove(SerializeMove(m_Move, animationStates));

                m_Jump = false;
            }
            else
            {
                if (autoPilot)
                {
                    bool[] falseArray = new bool[numberOfAnimations];
                    for (int i = 0; i < numberOfAnimations; i += 1)
                    {
                        falseArray[i] = false;
                    }
                    m_Character.Move(autoTarget - transform.position, falseArray);

                    bool[] animationStates = new bool[numberOfAnimations];
                    for (int i = 0; i < Utils.animations.Length; i += 1)
                    {
                        animationStates[i] = false;
                    }

                    if (Vector3.Distance(transform.position, autoTarget) < 0.1)
                    {
                        ArrivedAtAnimationDistance(animationStates);
                    }
                    else
                    {
                        GetComponent<UpdateMove>().UpdateCharacterMove(SerializeMove(autoTarget - transform.position, animationStates));
                    }
                }
            }
        }
    }

    private void ArrivedAtAnimationDistance(bool[] currentAnimationStates)
    {
        //handshake finishing action
        transform.LookAt(rotateTowardsTarget);
        //LOG("Animation State: " + currentAnimationStates.ToString());

        GetComponent<UpdateMove>().UpdateCharacterMove(SerializeMove(new Vector3(0, 0, 0), currentAnimationStates));

        System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

        // Update Move Here
        GetComponent<MoveSync>().SetLastAction(currentInteraction + "_" + cur_time.ToString());
        m_Character.StartAnimation(currentInteraction, !isAnimationLocal);

        canMove = true;
        autoPilot = false;
    }
}