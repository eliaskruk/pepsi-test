﻿using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using Mono.Data.SqliteClient;
using UnityEngine.UI;
using Eg_NFC;
/*using System.Data;*/

public class MainController : MonoBehaviour {

	public string dbName = "develop.db";
	private string appHash = "P3ps12016!";

#if UNITY_EDITOR
    private string responseURL = "http://localhost/pepsi_music/response/response.php";
	private string responseAssets = "http://localhost/pepsi_music/response/assets/images/";
#else
    private string responseURL = "http://www.thepastoapps.com/proyectos/pepsi_music/response/response.php";
    private string responseAssets = "http://www.thepastoapps.com/proyectos/pepsi_music/response/assets/images/";
#endif
    private string Uid;

	private float loadTime;
	private bool closeApp;
	private bool checkUpdate;
	private string errorChrs;

	public dbAccess db ;

	public UserData userData;
    public ContentData ContentData;

    public bool haveInet;
	public bool checkingCon = false;
    public int appVer = 1;

    //GPS
    public bool gps_active = false;
    public string userLat;
    public string userLng;

    //para debug
    public bool isDebug;
	public string sendDataDebug;
    public GameObject reporter;

	//notificaciones
	public notifications notificationsScript;

	//popup
	public GameObject popup;
	public GameObject popupText;
	public GameObject popupButton;

	//loading
	public GameObject loading;
    public GameObject Downloading;

	void OnGUI(){
        Screen.fullScreen = false;

        if (isDebug) {
			GUI.skin.label.fontSize = 20;
			GUI.Label (new Rect (0, Screen.height * 0.775f, Screen.width, Screen.height * 0.05f), "DEBUG : " + sendDataDebug);
		}
	}

	void createDb(){
		db.OpenDB(dbName);

		string[] cols = new string[]{"id", "regid", "plataforma", "fbid", "token", "nombre", "email" };
		string[] colTypes = new string[]{"INT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT" };
		db.CreateTable ("usuarios", cols, colTypes);

		cols = new string[]{"id", "titulo", "subtitulo", "contenido", "contenido_tipo", "contenidos_artistas_id", "tiempo_activo", "fecha_inicio", "fecha_fin", "imagen", "tipo_vencimiento", "precargado", "serverupdate" };
		colTypes = new string[]{"INT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT" };
		db.CreateTable ("contenidos", cols, colTypes);

        cols = new string[] { "id", "usuarios_id", "codigos_id", "contenidos_id", "fecha_entrada" };
        colTypes = new string[] { "INT", "TEXT", "TEXT", "TEXT", "TEXT" };
        db.CreateTable("contenidos_usuarios", cols, colTypes);

        cols = new string[] { "id", "artista", "descripcion", "imagen", "serverupdate" };
        colTypes = new string[] { "INT", "TEXT", "TEXT", "TEXT", "TEXT" };
        db.CreateTable("contenidos_artistas", cols, colTypes);

        cols = new string[] { "id", "codigo", "serverupdate" };
        colTypes = new string[] { "INT", "TEXT", "TEXT" };
        db.CreateTable("codigos", cols, colTypes);

        cols = new string[]{"id", "func", "sfields", "svalues"};
		colTypes = new string[]{"INT", "TEXT", "TEXT", "TEXT"};
		db.CreateTable ("sync", cols, colTypes);

		db.CloseDB();
	}

    //private bool isFirstInit = false;

	// Use this for initialization
	void Start () {

        Debug.Log("Path de la app: " + Application.persistentDataPath);

		Uid = "";
		isDebug = false;
		checkUpdate = true;
		loadTime = 0;
		db = GetComponent<dbAccess>();
		createDb ();

        

        //insertar primer contenido
        insertFirstContent();

        haveInet = false;
		checkConnection ();

		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		DontDestroyOnLoad (transform.gameObject);
        DontDestroyOnLoad(this.reporter);

        if (Uid == "") {
			if (PlayerPrefs.HasKey ("Uid")) {
				Uid = PlayerPrefs.GetString ("Uid");
			} else {
				Uid = SystemInfo.deviceUniqueIdentifier;
				PlayerPrefs.SetString ("Uid", Uid);
			}
		}

		if (!PlayerPrefs.HasKey ("config_alerts")) {
			PlayerPrefs.SetString ("config_alerts", "true");
		} else {
			string config_alerts = PlayerPrefs.GetString ("config_alerts");
			if(config_alerts == "false"){
				notificationsScript.disableNotifs();
			}
		}

        /*if (!PlayerPrefs.HasKey("isFirstInit"))
        {
            //isFirstInit = true;
            
        }
        else {
            
        }*/

        StartCoroutine (call_sync());
		

        //ej sync:
        /*string[] fields = {"puntos", "kilometros", "perros_id", "usuarios_id"};
		string[] values = {"100", "150", "454545" , "3"};
		insert_sync(fields, values, "perros_puntos");*/

        //PlayerPrefs.DeleteAll ();

        showLoading(true);
        StartCoroutine( checkUser() );

    }

    private void insertFirstContent() {

        //insertar artista
        string[] colsUsuarios = new string[] { "id", "artista", "descripcion", "imagen", "serverupdate" };
        string[] colsUsuariosValues = new string[] { "1", "Artista 1",
        "Descripcion del artista 1", "artista1.jpg", "2016-01-01" };

        db.OpenDB(dbName);
        db.InsertIgnoreInto("contenidos_artistas", colsUsuarios, colsUsuariosValues, "1");
        db.CloseDB();

        //tipo imagen

        colsUsuarios = new string[] { "id", "titulo", "subtitulo", "contenido", "contenido_tipo", "contenidos_artistas_id",
            "tiempo_activo", "fecha_inicio", "fecha_fin", "imagen", "tipo_vencimiento", "precargado", "serverupdate" };
        colsUsuariosValues = new string[] { "1", "titulo contenido 1", "Sub titulo contenido 1",
        "video_demo_proximity.mp4", "video", "1", "0",
        "2016-01-01", "2016-12-01", "contenidoi1.jpg", "fecha", "1", "2016-01-01"};

        db.OpenDB(dbName);
        db.InsertIgnoreInto("contenidos", colsUsuarios, colsUsuariosValues, "1");
        db.CloseDB();
    }

    /*void OnApplicationPause() {
        initNFC();
    }

    private Eg_NFC_DLL mNFC_Android;
    private string nfc_text;

    private void initNFC()
    {
        nfc_text = "initNFC";

        mNFC_Android = new Eg_NFC_DLL();
        mNFC_Android.SetCodingType("UTF-8");
        mNFC_Android.SetListener(gameObject, "OnReceivingMsg");
        // Default Status
        nfc_text = "Reading NFC";

        mNFC_Android.SetStatus(0);
    }

    private void OnReceivingMsg(string str)
    {
        Debug.Log("ReceivingMsg: " + str);
        nfc_text = mNFC_Android.GetID();
        nfc_text += ": ";
        nfc_text += mNFC_Android.GetTagData();

        nfc_text += " | ";
        nfc_text += str;

        NPBinding.UI.ShowAlertDialogWithSingleButton("Alerta!", "NFC detectado: " + nfc_text, "Aceptar", (string _buttonPressed) => {
            StartCoroutine(checkUser());
        });

        string[] strs = str.Split('_');

        switch (strs[0])
        {
            case Eg_NFC_Def.JarPushType.readID:
                //	do something
                break;
            case Eg_NFC_Def.JarPushType.readTag:
                //	do something
                break;
        }
    }*/

    private IEnumerator checkUser()
    {
        yield return new WaitForSeconds(1);
        if (userData.reg_id != "")
        {
            Debug.Log("regid: " + userData.reg_id);
            db.OpenDB(dbName);
            ArrayList result = db.BasicQueryArray("select id, email, nombre, fbid, token from usuarios where regid = '" + userData.reg_id + "' ");
            db.CloseDB();
            if (result.Count > 0){
                userData.populateUser( (string[])result[0] );
                StartCoroutine(get_updates());
                StartCoroutine(redirect("home", 2f));
            }
            else {
                if (haveInet)
                {
                    string[] colsUsuarios = new string[] { "plataforma", "regid" };
                    string[] colsUsuariosValues = new string[] { userData.plataforma, userData.reg_id };

                    sendData(colsUsuarios, colsUsuariosValues, "login_user");
                }
                else { // alerta de necesita conexion para loguear
                    NPBinding.UI.ShowAlertDialogWithSingleButton("Alerta!", "Necesitas conexion a internet para inicializar la aplicacion", "Aceptar", (string _buttonPressed) => {
                        StartCoroutine(checkUser());
                    });
                }
            }
        }
        else {
            Debug.Log("no regid");
            StartCoroutine(checkUser());
        }
    }


    void Awake () {
		DontDestroyOnLoad (transform.gameObject);
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		loadTime += Time.deltaTime;

		int roundedRestSeconds = Mathf.CeilToInt (loadTime);
		int displaySeconds = roundedRestSeconds % 60;
		
		/*if (Input.GetKeyDown(KeyCode.Escape)) {
			closeApp = true;
		}*/

		if (closeApp && loadTime > 1) {
			Application.Quit();
		}
		if (loadTime > 6) {
			loadTime = 0;
			checkUpdate = true;
			checkingCon = false;
		}

		if(displaySeconds == 5 && !checkingCon){
			checkingCon = true;
			checkConnection ();
		}

	}

	void checkConnection(){

		if (!haveInet) {
			WWWForm form = new WWWForm ();
			form.AddField ("appHash", appHash);
			form.AddField ("action", "check_connection");
			WWW www = new WWW (responseURL, form);
			StartCoroutine (WaitForRequest (www, "check_connection"));
		}
	}

	//send data inet
	public void sendData(string[] vars, string[] values, string action_, byte[] uploadImage = null){
		
		WWWForm form = new WWWForm();
		form.AddField("appHash", appHash);
		form.AddField("action", action_);
        form.AddField("uToken", userData.token);

        int index=0;
		sendDataDebug = "preparando variables";

		foreach (string vars_ in vars) {
			if(vars_ != "fileUpload"){
				try{
					form.AddField(vars_, values[index]);
				}catch(Exception e){
					sendDataDebug = "error en variable: "+index;
				}
			}else{
				form.AddBinaryData("fileUpload", uploadImage);
			}
			index++;
		}

		sendDataDebug = "iniciando WWW";
		
		WWW www = new WWW(responseURL, form);
		StartCoroutine(WaitForRequest(www, action_));
		//Debug.Log(www.text);
		
	}

	IEnumerator WaitForRequest(WWW www, string response){
		yield return www;
		
		// check for errors
		if (www.error == null){
			sendDataDebug = "WWW Ok!";

            //Debug.Log("WWW Ok!: " + www.text);

			IDictionary Wresponse = (IDictionary) MiniJSON.Json.Deserialize (www.text);

			string Wcontent_ = MiniJSON.Json.Serialize(Wresponse["content"]);
			string WarrayData_ = MiniJSON.Json.Serialize(Wresponse["arrayData"]);

			//Debug.Log("WWW content: " + Wcontent_);

			IDictionary Wresponse2 = (IDictionary) MiniJSON.Json.Deserialize ( Wcontent_ );
			IDictionary Wresponse3 = (IDictionary) MiniJSON.Json.Deserialize ( WarrayData_ );

			if((string)Wresponse["status"] == "error"){

				errorPopup((string)Wresponse2["mgs"], (string)Wresponse2["toclose"]);
			}else{

				if(response == "check_connection"){
					haveInet = true;
				}
                if (response == "login_user"){
                    userData.id = int.Parse((string)Wresponse3["id"]);
                    userData.token = (string)Wresponse3["token"];

                    saveUserData();
                    //call_updates("codigos");
                    get_codigos_usuarios();
                    //call_updates("contenidos_usuarios");

                    firstGetUpdates();

                    StartCoroutine(redirect("home", 4f));
                }
                    

                /*if(response == "login_facebook"){
					sendDataDebug = "entro a login_facebook";
					Debug.Log("login facebook OK! ID: "+ (string)Wresponse3["id"]);

					userData.id = int.Parse( (string)Wresponse3["id"] );

					if( (string)Wresponse2["hasArray"] != "0" ){
						string WarrayContent_ = MiniJSON.Json.Serialize(Wresponse["arrayContent"]);
						IDictionary WresponseContent = (IDictionary) MiniJSON.Json.Deserialize ( WarrayContent_ );
						
						
						for(int i = 1; i <= int.Parse( (string)Wresponse2["hasArray"] ); i++ ){
							IDictionary reponseContent = (IDictionary) MiniJSON.Json.Deserialize ( (string)WresponseContent[i.ToString()]  );
							
							userData.email = (string)reponseContent["email"];
							userData.nombre = (string)reponseContent["nombre"];
							userData.sexo = (string)reponseContent["sexo"];
							
							userData.foto = (string)reponseContent["foto"];
							
							try_download_persona_imagen((string)reponseContent["foto"]);
							
							saveUserData(true);
							
							//upload_user_foto();
							StartCoroutine( redirect("subir-foto", 3f) );
							download_personas();
							
						}
					}


				}*/

				if(response == "upload_perfil"){
					sendDataDebug = "imagen subida";

					db.OpenDB(dbName);

					string[] colsUsuarios = new string[]{ "foto" };
					string[] colsUsuariosValues = new string[]{ userData.foto};

					db.UpdateSingle("usuarios", "foto", userData.foto, "id" , userData.id.ToString());

					db.CloseDB();
				}

                if (response == "get_codigos_usuarios")
                {
                    Debug.Log("get_codigos_usuarios");

                    string WarrayContent_ = MiniJSON.Json.Serialize(Wresponse["arrayContent"]);
                    IDictionary WresponseContent = (IDictionary)MiniJSON.Json.Deserialize(WarrayContent_);

                    Debug.Log((string)Wresponse2["hasArray"]);
                    if ((string)Wresponse2["hasArray"] != "0")
                    {
                        for (int i = 1; i <= int.Parse((string)Wresponse2["hasArray"]); i++)
                        {
                            IDictionary reponseContent = (IDictionary)MiniJSON.Json.Deserialize((string)WresponseContent[i.ToString()]);
                            string newId = getNewId("contenidos_usuarios");

                            //id", "usuarios_id", "codigos_id", "contenidos_id", "fecha_entrada
                            string[] colsUsuarios = new string[] { "id", "usuarios_id", "codigos_id", "contenidos_id", "fecha_entrada" };
                            string[] colsUsuariosValues = new string[] { newId, (string)reponseContent["usuarios_id"],
                                            (string)reponseContent["codigos_id"], (string)reponseContent["contenidos_id"], (string)reponseContent["fecha_entrada"] };

                            db.OpenDB(dbName);
                            db.InsertIgnoreInto("contenidos_usuarios", colsUsuarios, colsUsuariosValues, (string)reponseContent["id"]);
                            db.CloseDB();
                        }
                    }
                }
                


                if (response == "get_updates"){

					//if((string)Wresponse2["mgs"] == "codigos_updated"){

					string WarrayContent_ = MiniJSON.Json.Serialize(Wresponse["arrayContent"]);
					IDictionary WresponseContent = (IDictionary) MiniJSON.Json.Deserialize ( WarrayContent_ );

					//Debug.Log((string)Wresponse2["hasArray"]);
					if( (string)Wresponse2["hasArray"] != "0" ){
						for(int i = 1; i <= int.Parse( (string)Wresponse2["hasArray"] ); i++ ){
							//Debug.Log("posicion: " + i);

							//string dada = MiniJSON.Json.Serialize(WresponseContent["1"]);
							IDictionary reponseContent = (IDictionary) MiniJSON.Json.Deserialize ( (string)WresponseContent[i.ToString()]  );
                                
							db.OpenDB(dbName);

							//ejemplo de update:
							    
							if((string)Wresponse2["mgs"] == "codigos_updated"){

                                string[] colsUsuarios = new string[]{"id", "codigo", "serverupdate"};
								string[] colsUsuariosValues = new string[]{ (string)reponseContent["id"], (string)reponseContent["codigo"], (string)reponseContent["serverupdate"] };
								
								db.InsertIgnoreInto("codigos", colsUsuarios, colsUsuariosValues, (string)reponseContent["id"]);
							}

                            if ((string)Wresponse2["mgs"] == "contenidos_updated")
                            {
                                //id", "titulo", "subtitulo", "contenido", "contenido_tipo", "tiempo_activo", "fecha_inicio", "fecha_fin", "imagen", "serverupdate

                                string ctype = "";
                                switch ((string)reponseContent["contenidos_tipos_id"]) {
                                    case "1": ctype = "imagen"; break;
                                    case "2": ctype = "video"; break;
                                    case "3": ctype = "audio"; break;
                                    case "4": ctype = "texto"; break;
                                    default: ctype = "texto"; break;
                                }

                                string[] colsUsuarios = new string[] { "id", "titulo", "subtitulo", "contenido", "contenido_tipo", "contenidos_artistas_id", "tiempo_activo", "fecha_inicio", "fecha_fin", "imagen", "tipo_vencimiento", "serverupdate" };
                                string[] colsUsuariosValues = new string[] { (string)reponseContent["id"], (string)reponseContent["titulo"], (string)reponseContent["subtitulo"],
                                    (string)reponseContent["contenido"], ctype, (string)reponseContent["contenidos_artistas_id"], (string)reponseContent["tiempo_activo"],
                                    (string)reponseContent["fecha_inicio"], (string)reponseContent["fecha_fin"], (string)reponseContent["imagen"], (string)reponseContent["tipo_vencimiento"], (string)reponseContent["serverupdate"]};

                                if ((string)reponseContent["contenido_tipo"] != "texto") {
                                    downloadFile((string)reponseContent["contenido"]);
                                }

                                if ((string)reponseContent["imagen"] != "" && (string)reponseContent["imagen"] != "null") {
                                    downloadFile((string)reponseContent["imagen"]);
                                }

                                db.InsertIgnoreInto("contenidos", colsUsuarios, colsUsuariosValues, (string)reponseContent["id"]);
                            }

                            if ((string)Wresponse2["mgs"] == "contenidos_artistas_updated")
                            {
                                //id", "artista", "descripcion", "imagen", "serverupdate

                                string[] colsUsuarios = new string[] { "id", "artista", "descripcion", "imagen", "serverupdate" };
                                string[] colsUsuariosValues = new string[] { (string)reponseContent["id"], (string)reponseContent["artista"],
                                    (string)reponseContent["descripcion"], (string)reponseContent["imagen"], (string)reponseContent["serverupdate"] };

                                downloadFile((string)reponseContent["imagen"]);

                                db.InsertIgnoreInto("contenidos_artistas", colsUsuarios, colsUsuariosValues, (string)reponseContent["id"]);
                            }
                            

                            db.CloseDB();
							
						}

						Debug.Log("updated: " + (string)Wresponse2["mgs"]);
					}
					//}
				}

				if(response == "sync"){
					db.OpenDB(dbName);
					db.BasicQueryInsert("delete from sync where id = '" +(string)Wresponse3["id"]+ "' ");
					db.CloseDB();
				}
			}


		} else {
			haveInet = false;
			sendDataDebug = "WWW Error: "+www.error;
			Debug.Log("WWW Error: "+ www.error);
		}
	}

	public IEnumerator redirect(string escene_, float seconds){
		yield return new WaitForSeconds (seconds);
		
		Debug.Log ("redirect escene: " + escene_ + " en " + seconds);
		showLoading(false);
		Application.LoadLevel (escene_);
	}

    /*private void populateUserData(IDictionary values){
		userData.email = (string)values["email"];
		userData.nombre = (string)values["nombre"];
		userData.fecha_nacimiento = (string)values["fecha_nacimiento"];
		userData.sexo = (string)values["sexo"];

		saveUserData (false);
	}*/

    private void saveUserData(){
        string[] colsUsuarios = new string[] { "id", "regid", "plataforma", "token" };
        string[] colsUsuariosValues = new string[] { userData.id.ToString(), userData.reg_id, userData.plataforma, userData.token };

        db.OpenDB(dbName);
        db.InsertIntoSpecific("usuarios", colsUsuarios, colsUsuariosValues);
        db.CloseDB();
    }


    /*private void saveUserData(bool isfb){
		sendDataDebug = "entro a saveUserData";
		db.OpenDB(dbName);
		
		string[] colsUsuarios = new string[]{ "id", "email", "nombre", "fbid", "plataforma", "regid"};

		ArrayList result = new ArrayList();
		if (isfb) {
			try{
				result = db.BasicQueryArray ("select fbid from usuarios where fbid = '" + userData.fbid + "' ");
			}catch(Exception e){
				sendDataDebug = "error con db";
			}
		} else {
			result = db.BasicQueryArray ("select email from usuarios where email = '"+userData.email+"' ");
		}

		string[] colsUsuariosValues = new string[]{ userData.id.ToString(), userData.email, userData.nombre, userData.fbid, userData.fecha_nacimiento, userData.sexo };
		
		if (result.Count == 0) {
			sendDataDebug = "count = 0 inserto usuario";
			db.InsertIntoSpecific ("usuarios", colsUsuarios, colsUsuariosValues);
		}

		db.CloseDB();
	}*/

	/*private void download_personas(){
		if (userData.id != 0) {
			string[] colsUsuarios = new string[]{ "usuarios_id" };
			string[] colsUsuariosValues = new string[]{ userData.id.ToString () };
		
			sendData (colsUsuarios, colsUsuariosValues, "get_personas");
		}
	}*/

	private void try_download_persona_imagen(string foto_){
		string filepath = Application.persistentDataPath + "/" + foto_;
		if (!File.Exists (filepath)) {
			StartCoroutine( downloadImg(foto_) );
		}
	}

	IEnumerator downloadImg (string image_name){
		if (image_name != "") {
			Texture2D texture = new Texture2D (1, 1);
			Debug.Log ("try download image: " + responseAssets + image_name);
			WWW www = new WWW (responseAssets + image_name);
			yield return www;
			www.LoadImageIntoTexture (texture);
		
			byte[] ImgBytes = texture.EncodeToPNG ();
		
			File.WriteAllBytes (Application.persistentDataPath + "/" + image_name, ImgBytes);
		}
	}

    public void downloadFile(string nombre_, bool isfromhome = false)
    {
        string filepath = Application.persistentDataPath + "/" + nombre_;
        if (!File.Exists(filepath))
        {
            StartCoroutine(downloadCr(nombre_, isfromhome));
        }
    }

    public bool checkFileExist(string nombre_) {
        string filepath = Application.persistentDataPath + "/" + nombre_;
        return File.Exists(filepath);
    }

    public string checkFileExist2(string nombre_)
    {
        string filepath = Application.streamingAssetsPath + "/" + nombre_;
        if (!File.Exists(filepath))
        {
            filepath = Application.persistentDataPath + "/" + nombre_;
            if (File.Exists(filepath)) {
                return filepath;
            } else {
                return "";
            }
        }
        else {
            return filepath;
        }
    }

    string progress;
    public bool isDonwloading = false;

    IEnumerator downloadCr(string nombre_, bool isfromhome = false)
    {
        if (nombre_ != "")
        {
            Texture2D texture = new Texture2D(1, 1);
            Debug.Log("try download image: " + responseAssets + nombre_);
            WWW www = new WWW(responseAssets + nombre_);
            //yield return www;

            while (!www.isDone)
            {
                progress = "downloaded " + (www.progress * 100).ToString() + "%...";
                Debug.Log(progress);
                yield return null;
            }

            string fullPath = Application.persistentDataPath + "/" + nombre_;
            File.WriteAllBytes(fullPath, www.bytes);

            if (isfromhome) {
                isDonwloading = false;
                Downloading.SetActive(false);
                //StartCoroutine( redirect("content", 1f) );
            }
        }
    }

    public void upload_user_foto(){
		//subir imagen
		byte[] fileData = File.ReadAllBytes (Application.persistentDataPath + "/" + userData.foto);
		
		Debug.Log ("try upload: imagen usuario");
		string[] cols2 = new string[]{"usuarios_id", "fileUpload", "usuario_foto"};
		string[] data2 = new string[]{userData.id.ToString (), "imagen_usuario", userData.foto };
		try {
			sendData (cols2, data2, "upload_perfil", fileData);
		} catch (IOException e) {
			Debug.Log (e);
		}
	}

	public int generateId(){
		int timestamp = (int)Math.Truncate((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		return timestamp;
	}

	public void loginFacebook(){
		
		string[] colsUsuarios = new string[]{ "email", "nombre", "fbid",/* "fecha_nacimiento", "sexo",*/ "plataforma", "regid"};
		string[] colsUsuariosValues = new string[]{ userData.email, userData.nombre, userData.fbid, /*userData.fecha_nacimiento, userData.sexo, */userData.plataforma, userData.reg_id };
		
		sendData (colsUsuarios, colsUsuariosValues, "login_facebook");
	}

    private void firstGetUpdates() {
        call_updates("codigos");
        call_updates("contenidos_artistas");
        call_updates("contenidos");

        StartCoroutine(get_updates());
        //StartCoroutine(endFirstUpdates());
    }

    /*private IEnumerator endFirstUpdates()
    {
        yield return new WaitForSeconds(6);
        PlayerPrefs.SetString("isFirstInit", "1");
        //isFirstInit = true;
    }*/


    private IEnumerator get_updates(){
		yield return new WaitForSeconds (6);

		//ej de call updates

		call_updates ("codigos");
        call_updates("contenidos_artistas");
        call_updates("contenidos");

        //download_personas();
        StartCoroutine (get_updates ());
	}

    public void get_codigos_usuarios()
    {
        if (haveInet)
        {

            string[] cols = new string[] { "usuarios_id", "reg_id" };
            string[] values = new string[] { userData.id.ToString(), userData.reg_id };
            sendData(cols, values, "get_codigos_usuarios");
        }
    }

    public void call_updates( string table ){
		if (haveInet) {
			db.OpenDB (dbName);

			ArrayList result = db.BasicQueryArray ("select serverupdate from " + table + " order by serverupdate DESC limit 1");
			string serverUpdate = "2015-01-01";
			if (result.Count > 0) {
				serverUpdate = ((string[])result [0]) [0];
			}
			db.CloseDB ();
			string[] cols = new string[]{ "table", "serverupdate"};
			string[] values = new string[]{ table, serverUpdate};
            //Debug.Log("call_updates: " + table );
			sendData (cols, values, "get_updates");
		}
	}

	private IEnumerator call_sync(){
		yield return new WaitForSeconds (4);
		sync ();
	}

	public void sync(){
		//Debug.Log ("sync....");
		//sync foto perro
		db.OpenDB(dbName);

		ArrayList result = db.BasicQueryArray ("select id, func, sfields, svalues from sync order by id ASC limit 1");
		if (result.Count > 0) {
			if(haveInet){
				string[] cols = new string[]{ "id", "func", "fields", "values"};
				string[] values = new string[]{ ((string[])result [0])[0] , ((string[])result [0])[1], ((string[])result [0])[2], ((string[])result [0])[3]};
				sendData (cols, values, "sync");
			}
			//((string[])result [0])[0];
		}

		db.CloseDB();
		StartCoroutine (call_sync ());
	}

	public void insert_sync(string[] fields, string[] values, string sync_func){

		string fields_json = MiniJSON.Json.Serialize(fields);
		string values_json = MiniJSON.Json.Serialize(values);

		string newSyncId = getSyncNewId ();

		//Debug.Log ("insertar en sync fields: " + fields_json + "values: " + values_json + " func: " + sync_func);

		string[] colsF = new string[]{ "id", "func", "sfields", "svalues"};
		string[] colsV = new string[]{ newSyncId, sync_func, fields_json, values_json };

		db.OpenDB (dbName);
		db.InsertIntoSpecific("sync", colsF, colsV);
		db.CloseDB ();
	}

	private string getSyncNewId(){
		db.OpenDB(dbName);
		ArrayList result = db.BasicQueryArray ("select id from sync order by id DESC limit 1");
		db.CloseDB();
		
		string newId = "1";
		
		if (result.Count > 0) {
			newId = ((string[])result [0]) [0];
			int newIdInt = int.Parse(newId)+1;
			newId = newIdInt.ToString();
		}
		
		return newId;
	}

    public string getNewId(string table_){
        db.OpenDB(dbName);
        ArrayList result = db.BasicQueryArray("select id from "+ table_ + " order by id DESC limit 1");
        db.CloseDB();

        string newId = "1";

        if (result.Count > 0)
        {
            newId = ((string[])result[0])[0];
            int newIdInt = int.Parse(newId) + 1;
            newId = newIdInt.ToString();
        }

        return newId;
    }


    public bool validEmail(string emailaddress){
		return System.Text.RegularExpressions.Regex.IsMatch(emailaddress, @"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
	}

	/*public void errorPopup(string error = "Error", string toclose = ""){

		popup.SetActive (true);
		popupText.GetComponent<Text> ().text = error;
		if (toclose == "1") {
			popupButton.SetActive (false);
		} else {
			popupButton.SetActive (true);
		}
	}*/



	public void errorPopup(string error = "Error", string toclose = ""){
		
		string btnText = "Aceptar";
		/*if (toclose != "" && toclose != null) {
			btnText = "Entiendo";
			errorChrs = error;
		}*/
		
		NPBinding.UI.ShowAlertDialogWithSingleButton ("Alerta!", error, btnText, (string _buttonPressed)=>{
			if (_buttonPressed == "Aceptar") {
				Debug.Log("aceptado");
			}
			if (_buttonPressed == "Entiendo") {
				errorPopup(errorChrs, "1");
			}
		}); 
	}

	public void showLoading(bool show = true){
		loading.SetActive (show);
	}

	public void closePopup(){
		popup.SetActive (false);
	}

    public Sprite spriteFromFileP(string image_)
    {
        Debug.Log("spriteFromFile: " + image_);
        Sprite sprite = new Sprite();
        if (image_ != "")
        {

            if (File.Exists(image_))
            {

                byte[] fileData = File.ReadAllBytes(image_);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

                Debug.Log(tex.width + "x" + tex.height);
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0f));
            }
            else {
                sprite = defaultSprite();
            }
        }
        else {
            sprite = defaultSprite();
        }
        return sprite;
    }

    private Sprite defaultSprite() {
        Sprite sprite = new Sprite();
        Texture2D tex = Resources.Load("default") as Texture2D;
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0f));
        return sprite;
    }

    public Sprite spriteFromFile(string image_){
		Debug.Log ("spriteFromFile: " + image_);
		Sprite sprite = new Sprite ();
		if (image_ != "") {

			byte[] fileData = File.ReadAllBytes (Application.persistentDataPath + "/" + image_);
			Texture2D tex = new Texture2D (2, 2);
			tex.LoadImage (fileData); //..this will auto-resize the texture dimensions.

			Debug.Log (tex.width + "x" + tex.height);
			sprite = Sprite.Create (tex, new Rect (0, 0, tex.width, tex.height), new Vector2 (0f, 0f));

		} else {
			Texture2D tex = Resources.Load("default") as Texture2D;
			sprite = Sprite.Create (tex, new Rect (0, 0, tex.width, tex.height), new Vector2 (0f, 0f));
		}
		return sprite;
	}

	public Sprite spriteSquareFromFile(string image_){
		Debug.Log ("spriteFromFile: " + image_);
		Sprite sprite = new Sprite ();
		if (image_ != "") {
			
			byte[] fileData = File.ReadAllBytes (Application.persistentDataPath + "/" + image_);
			Texture2D tex = new Texture2D (2, 2);
			tex.LoadImage (fileData); //..this will auto-resize the texture dimensions.
			
			//convertirla en cuadrado
			Texture2D texSq = new Texture2D(2, 2, TextureFormat.ARGB32, false);;
			
			if(tex.width > tex.height){
				
				int restText = tex.width - tex.height;
				int restText2 =  restText/2 ;
				texSq = new Texture2D(tex.height, tex.height, TextureFormat.ARGB32, false);
				
				int xi = 1;
				for (var y = 1; y <= texSq.height; y++) {
					xi = 1;
					for (var x = restText2; x < ( tex.width - restText2 ); x++) {
						texSq.SetPixel (xi, y, tex.GetPixel (x, y));
						xi ++;
					}
				}
			}
			
			if(tex.height > tex.width){
				
				int restText = tex.height - tex.width;
				int restText2 = restText/2 ;
				
				texSq = new Texture2D(tex.width, tex.width, TextureFormat.ARGB32, false);
				
				int yi = 1;
				for (var x = 1; x <= texSq.width; x++) {
					yi = 1;
					for (var y = restText2; y < ( tex.height - restText2 ); y++) {
						texSq.SetPixel (x, yi, tex.GetPixel (x, y));
						yi ++;
					}
				}
			}
			
			texSq.Apply();
			
			sprite = Sprite.Create (texSq, new Rect (0, 0, texSq.width, texSq.height), new Vector2 (0f, 0f));
			
		} else {
			Texture2D tex = Resources.Load("default (2)") as Texture2D;
			sprite = Sprite.Create (tex, new Rect (0, 0, tex.width, tex.height), new Vector2 (0f, 0f));
		}
		return sprite;
	}

	public IEnumerator saveTextureToFile(Texture2D /*savedTexture */loadTexture, string fileName, char tosave){
		yield return new WaitForSeconds(0.5f);

		int newWidth = 600;
		int newHeigth =  (newWidth * loadTexture.height / loadTexture.width) ;

		Texture2D savedTexture = ScaleTexture (loadTexture, newWidth, newHeigth);

		Debug.Log ("guardar textura en imagen: " + fileName + " " + savedTexture.width + "x" + savedTexture.height);

		Texture2D newTexture = new Texture2D(savedTexture.width, savedTexture.height, TextureFormat.ARGB32, false);
		
		newTexture.SetPixels(0,0, savedTexture.width, savedTexture.height, savedTexture.GetPixels());
		newTexture.Apply();
		if(tosave == 'u'){
			userData.ImgBytes = newTexture.EncodeToPNG ();
			userData.temp_img = fileName;
			
			File.WriteAllBytes (Application.persistentDataPath + "/" + userData.temp_img, userData.ImgBytes);
			Debug.Log (Application.persistentDataPath + "/" + userData.temp_img);
		}
	}

	public string getActualDate(){
		return DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
	}

    public string getActualDate2()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    public string getHour(){
		return DateTime.Now.ToString ("HH");
	}


	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result=new Texture2D(targetWidth,targetHeight,source.format,false);
		float incX=(1.0f / (float)targetWidth);
		float incY=(1.0f / (float)targetHeight);
		for (int i = 0; i < result.height; ++i) {
			for (int j = 0; j < result.width; ++j) {
				Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
				result.SetPixel(j, i, newColor);
			}
		}
		result.Apply();
		return result;
	}

}
