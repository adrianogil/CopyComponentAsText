using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

using System;
using System.Reflection;

public class SerializableGameObjectReference : XMLSerializableObject<SerializableGameObjectReference>
{
    [XmlAttribute("GameObjectReferenceId")]
    public int gameObjectReferenceId;

    [XmlAttribute("GameObjectReferenceName")]
    public string gameObjectReferenceName;
}

public class SerializableComponentReference : XMLSerializableObject<SerializableComponentReference>
{
    [XmlAttribute("ComponentReferenceId")]
    public int componentReferenceId;

    [XmlAttribute("ComponentReferenceName")]
    public string componentReferenceName;
}

public class SerializableVector3 : XMLSerializableObject<SerializableVector3>
{
    [XmlAttribute("vecX")]
    public float x;

    [XmlAttribute("vecY")]
    public float y;

    [XmlAttribute("vecZ")]
    public float z;

    public SerializableVector3() {}
    public SerializableVector3(Vector3 vector) { x = vector.x; y = vector.y; z = vector.z;}
    public Vector3 GetVector3() { return new Vector3(x,y,z); }
}

public class SerializableVector2 : XMLSerializableObject<SerializableVector2>
{
    [XmlAttribute("vecX")]
    public float x;

    [XmlAttribute("vecY")]
    public float y;

    public SerializableVector2() {}
    public SerializableVector2(Vector2 vector) { x = vector.x; y = vector.y;}
    public Vector2 GetVector2() { return new Vector2(x,y); }
}

public class SerializableComponentProperty : XMLSerializableObject<SerializableComponentProperty>
{
    [XmlAttribute("PropertyName")]
    public string propertyName;

    [XmlAttribute("PropertyValue")]
    public string propertyValue;

    [XmlAttribute("PropertyType")]
    public int propertyType;
}

public class SerializableGameObject : XMLSerializableObject<SerializableGameObject>
{
    [XmlAttribute("GameObjectName")]
    public string name;

    [XmlAttribute("GameObjectId")]
    public int gameObjectId;

    [XmlArray("GameObjectComponents")]
    [XmlArrayItem("Component")]
    public List<SerializableComponent> components = new List<SerializableComponent>();

    [XmlArray("SerializableChildren")]
    [XmlArrayItem("SerializableChild")]
    public List<SerializableGameObject> children = new List<SerializableGameObject>();
}

// <?xml version="1.0" encoding="utf-16"?>
// <SerializableGameObject xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
//   <GameObjectComponents>
//     <Component ComponentName="Transform" />
//     <Component ComponentName="UIButton" />
//     <Component ComponentName="VRInteractiveItem" />
//     <Component ComponentName="NormalButton" />
//     <Component ComponentName="TweenScale" />
//     <Component ComponentName="SphereCollider" />
//     <Component ComponentName="Rigidbody" />
//   </GameObjectComponents>
// </SerializableGameObject>

public class ComponentAsText : EditorWindow
{
    // In order to perform Serialization of GameObject references
    public static List<SerializableGameObjectReference> gameObjectsSerializableReferences = new List<SerializableGameObjectReference>();
    public static List<GameObject> referencedGameObjects = new List<GameObject>();
    public static List<SerializableComponentProperty> objectReferenceProperties = new List<SerializableComponentProperty>();
    // In order to perform Serialization of Component references
    public static List<SerializableComponentReference> componentsSerializableReferences = new List<SerializableComponentReference>();
    public static List<Component> referencedComponents = new List<Component>();
    public static List<SerializableComponentProperty> componentReferenceProperties = new List<SerializableComponentProperty>();

    // In order to perform Deserialization of GameObject references
    public static List<SerializableGameObjectReference> gameObjectsDeserializableReferences = new List<SerializableGameObjectReference>();
    public static List<Component> componentsToSetGameObject = new List<Component>();
    public static List<string> gameObjectPropertyNames = new List<string>();

    // In order to perform Deserialization of Component references
    public static List<SerializableComponentReference> componentsDeserializableReferences = new List<SerializableComponentReference>();
    public static List<Component> componentsToSetComponent= new List<Component>();
    public static List<string> componentPropertyNames = new List<string>();

    public static int currentGameObjectId = -1;
    public static int currentComponentId = -1;

    [MenuItem("GameObject/Tools/Copy as Text", false, 0)]
    static void CopyGameObject()
    {
        Copy();
    }

    [MenuItem("GameObject/Tools/Paste from Text", false, 0)]
    static void PasteGameObject()
    {
        Paste();
    }

    [MenuItem("CONTEXT/Transform/Copy as Text", false, 150)]
    static void CopyTransform()
    {
        Transform target = Selection.activeTransform;

        if (target != null)
        {
            SerializableComponent sComponent = GetSerializableComponent(target);
            if (sComponent != null)
            {
                EditorGUIUtility.systemCopyBuffer = sComponent.GetString();
            }
        }
    }

    [MenuItem("CONTEXT/Transform/Paste Values from Text", false, 150)]
    static void PasteTransform()
    {
        Transform target = Selection.activeTransform;

        if (target != null)
        {
            SerializableComponent sComponent = SerializableComponent.LoadFromText(EditorGUIUtility.systemCopyBuffer);
            if (sComponent != null)
            {
                sComponent.ApplySerializedDataTo(target);
            }
        }
    }

    [MenuItem("GameObject/Copy all components as Text")]
    static void Copy()
    {
        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 0f);

        objectReferenceProperties.Clear();
        gameObjectsSerializableReferences.Clear();
        referencedGameObjects.Clear();
        componentsSerializableReferences.Clear();
        referencedComponents.Clear();

        currentGameObjectId = 0;
        currentComponentId = 0;

        GameObject target = Selection.activeGameObject;

        SerializableGameObject sGObject = GetSerializableGameObject(target);

        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 0.5f);

        GetReferencesForSerialization(target, sGObject);

        EditorUtility.DisplayProgressBar("Copying GameObjects", "...", 1f);

        if (sGObject != null)
        {
            EditorGUIUtility.systemCopyBuffer = sGObject.GetString();
        }

        EditorUtility.ClearProgressBar();
    }

    static void VerifyReferencedGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        for (int i = 0; i < referencedGameObjects.Count; i++)
        {
            if (gameObject == referencedGameObjects[i])
            {
                gameObjectsSerializableReferences[i].gameObjectReferenceId = sGObject.gameObjectId;
                objectReferenceProperties[i].propertyValue = gameObjectsSerializableReferences[i].GetString();
            }
        }
    }

    static void VerifyReferencedComponents(Component component, SerializableComponent sComponent)
    {
        for (int i = 0; i < referencedComponents.Count; i++)
        {
            if (component == referencedComponents[i])
            {
                componentsSerializableReferences[i].componentReferenceId = sComponent.componentId;
                componentReferenceProperties[i].propertyValue = componentsSerializableReferences[i].GetString();
            }
        }
    }

    static void GetReferencesForSerialization(GameObject gameObject, SerializableGameObject sGObject)
    {
        if (gameObject == null)
        {
            return;
        }

        VerifyReferencedGameObject(gameObject, sGObject);

        Component[] copiedComponents = gameObject.GetComponents<Component>();
        if (copiedComponents != null)
        {
            for (int c = 0; c < copiedComponents.Length; c++)
            {
                VerifyReferencedComponents(copiedComponents[c], sGObject.components[c]);
            }
        }

        if (gameObject.transform.childCount > 0)
        {
            for (int s = 0; s < gameObject.transform.childCount; s++)
            {
                GetReferencesForSerialization(gameObject.transform.GetChild(s).gameObject, sGObject.children[s]);
            }
        }
    }

    static SerializableComponent GetSerializableComponent(Component component)
    {
        SerializableComponent sComponent = new SerializableComponent();
        sComponent.componentId = currentComponentId;
        sComponent.componentName = component.GetType ().Name;

        currentComponentId++;

        SerializableComponentProperty sProp;

        Type type = component.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
             if (pinfo.CanWrite) {
                 // try {
                 //     Debug.Log(sComponent.componentName + "." + pinfo.Name + " = " + pinfo.GetValue(copiedComponents[c], null).ToString() +
                 //        " [" + pinfo.PropertyType.ToString() + "] ");
                 // }
                 // catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.

                try {

                    if (pinfo.PropertyType == typeof(string) ||
                         pinfo.PropertyType == typeof(int)    ||
                         pinfo.PropertyType == typeof(float)  ||
                         pinfo.PropertyType == typeof(bool)  )
                     {
                        sProp = new SerializableComponentProperty();
                        sProp.propertyName = pinfo.Name;
                        if (pinfo.PropertyType == typeof(string) && pinfo.GetValue(component, null) == null)
                        {
                            sProp.propertyValue = "";
                        }
                        else
                        {
                            sProp.propertyValue = pinfo.GetValue(component, null).ToString();
                        }

                        if (pinfo.PropertyType == typeof(string))
                            sProp.propertyType = 0;
                        else if (pinfo.PropertyType == typeof(int))
                            sProp.propertyType = 1;
                        else if (pinfo.PropertyType == typeof(float))
                            sProp.propertyType = 2;
                        else if (pinfo.PropertyType == typeof(bool))
                            sProp.propertyType = 3;
                        sComponent.properties.Add(sProp);
                     }
                     else if (pinfo.PropertyType == typeof(Vector3))
                     {
                        sProp = new SerializableComponentProperty();
                        sProp.propertyName = pinfo.Name;
                        sProp.propertyValue = new SerializableVector3((Vector3)pinfo.GetValue(component, null)).GetString();
                        sProp.propertyType = 4;
                        sComponent.properties.Add(sProp);
                     }
                     else if (pinfo.PropertyType == typeof(Vector2))
                     {
                        sProp = new SerializableComponentProperty();
                        sProp.propertyName = pinfo.Name;
                        sProp.propertyValue = new SerializableVector2((Vector2)pinfo.GetValue(component, null)).GetString();
                        sProp.propertyType = 5;
                        sComponent.properties.Add(sProp);
                     }
                     else if (pinfo.PropertyType == typeof(GameObject) && pinfo.GetValue(component, null) != null)
                     {
                        SerializableGameObjectReference sGObjectReference = new SerializableGameObjectReference()
                        {
                            gameObjectReferenceName = ((GameObject)pinfo.GetValue(component, null)).name
                        };
                        gameObjectsSerializableReferences.Add(sGObjectReference);
                        referencedGameObjects.Add((GameObject)pinfo.GetValue(component, null));

                        sProp = new SerializableComponentProperty();
                        sProp.propertyName = pinfo.Name;
                        //sProp.propertyValue
                        sProp.propertyType = 6;
                        sComponent.properties.Add(sProp);
                        objectReferenceProperties.Add(sProp);
                     }
                     else if ((pinfo.PropertyType == typeof(Component) || pinfo.PropertyType.IsSubclassOf(typeof(Component))) &&
                        pinfo.GetValue(component, null) != null)
                     {
                        SerializableComponentReference sComponentReference = new SerializableComponentReference()
                        {
                            componentReferenceName = ((Component)pinfo.GetValue(component, null)).GetType ().Name
                        };
                        componentsSerializableReferences.Add(sComponentReference);
                        referencedComponents.Add((Component)pinfo.GetValue(component, null));

                        sProp = new SerializableComponentProperty();
                        sProp.propertyName = pinfo.Name;
                        //sProp.propertyValue
                        sProp.propertyType = 7;
                        sComponent.properties.Add(sProp);
                        componentReferenceProperties.Add(sProp);
                     }
                 }
                 catch {}
             }
         }

         FieldInfo[] finfos = type.GetFields(flags);
         foreach (var finfo in finfos) {
             if (finfo.FieldType == typeof(string) ||
                     finfo.FieldType == typeof(int)    ||
                     finfo.FieldType == typeof(float)  ||
                     finfo.FieldType == typeof(bool)  )
                 {
                    sProp = new SerializableComponentProperty();
                    sProp.propertyName = finfo.Name;
                    if (finfo.FieldType == typeof(string) && finfo.GetValue(component) == null)
                    {
                        sProp.propertyValue = "";
                    }
                    else
                    {
                        sProp.propertyValue = finfo.GetValue(component).ToString();
                    }

                    if (finfo.FieldType == typeof(string))
                        sProp.propertyType = 0;
                    else if (finfo.FieldType == typeof(int))
                        sProp.propertyType = 1;
                    else if (finfo.FieldType == typeof(float))
                        sProp.propertyType = 2;
                    else if (finfo.FieldType == typeof(bool))
                        sProp.propertyType = 3;
                    sComponent.properties.Add(sProp);
                 }
                 else if (finfo.FieldType == typeof(Vector3))
                 {
                    sProp = new SerializableComponentProperty();
                    sProp.propertyName = finfo.Name;
                    sProp.propertyValue = new SerializableVector3((Vector3)finfo.GetValue(component)).GetString();
                    sProp.propertyType = 4;
                    sComponent.properties.Add(sProp);
                 }
                 else if (finfo.FieldType == typeof(Vector2))
                 {
                    sProp = new SerializableComponentProperty();
                    sProp.propertyName = finfo.Name;
                    sProp.propertyValue = new SerializableVector2((Vector2)finfo.GetValue(component)).GetString();
                    sProp.propertyType = 5;
                    sComponent.properties.Add(sProp);
                 }
                 else if (finfo.FieldType == typeof(GameObject) && ((GameObject)finfo.GetValue(component)) != null)
                 {
                    SerializableGameObjectReference sGObjectReference = new SerializableGameObjectReference()
                    {
                        gameObjectReferenceName = ((GameObject)finfo.GetValue(component)).name
                    };
                    gameObjectsSerializableReferences.Add(sGObjectReference);
                    referencedGameObjects.Add((GameObject)finfo.GetValue(component));

                    sProp = new SerializableComponentProperty();
                    sProp.propertyName = finfo.Name;
                    //sProp.propertyValue
                    sProp.propertyType = 6;
                    sComponent.properties.Add(sProp);
                    objectReferenceProperties.Add(sProp);
                 }
                 else if ((finfo.FieldType == typeof(Component) || finfo.FieldType.IsSubclassOf(typeof(Component))) &&
                    finfo.GetValue(component) != null)
                 {
                    SerializableComponentReference sComponentReference = new SerializableComponentReference()
                    {
                        componentReferenceName = ((Component)finfo.GetValue(component)).GetType ().Name
                    };
                    componentsSerializableReferences.Add(sComponentReference);
                    referencedComponents.Add((Component)finfo.GetValue(component));

                    sProp = new SerializableComponentProperty();
                    sProp.propertyName = finfo.Name;
                    //sProp.propertyValue
                    sProp.propertyType = 7;
                    sComponent.properties.Add(sProp);
                    componentReferenceProperties.Add(sProp);
                 }
        }

        return sComponent;
    }

    static SerializableGameObject GetSerializableGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return null;
        }

        Component[] copiedComponents = gameObject.GetComponents<Component>();

        if (copiedComponents != null)
        {
            SerializableGameObject sGObject = new SerializableGameObject();
            sGObject.gameObjectId = currentGameObjectId;
            sGObject.name = gameObject.name;
            SerializableComponent sComponent;

            currentGameObjectId++;

            for (int c = 0; c < copiedComponents.Length; c++)
            {
                sComponent = GetSerializableComponent(copiedComponents[c]);
                sGObject.components.Add(sComponent);
            }

            if (gameObject.transform.childCount > 0)
            {
                SerializableGameObject sChildrenGObject;

                for (int s = 0; s < gameObject.transform.childCount; s++)
                {
                    sChildrenGObject = GetSerializableGameObject(gameObject.transform.GetChild(s).gameObject);
                    sGObject.children.Add(sChildrenGObject);
                }
            }

            return sGObject;
        }

        return null;
    }

    [MenuItem("GameObject/Paste all components from Text")]
    static void Paste()
    {
        EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0f);

        string componentsText = EditorGUIUtility.systemCopyBuffer;

        //Debug.Log("gil - ComponentAsText::Paste - " + componentsText);

        gameObjectsDeserializableReferences.Clear();
        componentsToSetGameObject.Clear();
        gameObjectPropertyNames.Clear();

        componentsDeserializableReferences.Clear();
        componentsToSetComponent.Clear();
        componentPropertyNames.Clear();

        SerializableGameObject sGObject = SerializableGameObject.LoadFromText(componentsText);

        EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0.3f);

        if (sGObject != null)
        {
            //Debug.Log("gil - ComponentAsText::Paste - " + Selection.gameObjects.Length);

            foreach (var targetGameObject in Selection.gameObjects)
            {
                if (!targetGameObject) continue;

                Debug.Log("gil - ComponentAsText::Paste - GameObject.name = " + targetGameObject.name);

                PasteSerializableGameObject(targetGameObject, sGObject);

                EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 0.6f);

                GetReferencesForDeserialization(targetGameObject, sGObject);

                EditorUtility.DisplayProgressBar("Paste GameObjects", "...", 1f);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    static void VerifyGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        for (int i = 0; i < gameObjectsDeserializableReferences.Count; i++)
        {
            if (sGObject.gameObjectId == gameObjectsDeserializableReferences[i].gameObjectReferenceId)
            {
                Type type = componentsToSetGameObject[i].GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
                PropertyInfo[] pinfos = type.GetProperties(flags);
                foreach (var pinfo in pinfos) {
                    if (pinfo.CanWrite && pinfo.Name == gameObjectPropertyNames[i]) {
                        try {
                            pinfo.SetValue(componentsToSetGameObject[i], gameObject, null);
                        } catch {}
                    }
                }

                FieldInfo[] finfos = type.GetFields(flags);
                foreach (var finfo in finfos) {
                    if (finfo.Name == gameObjectPropertyNames[i]) {
                        try {
                            finfo.SetValue(componentsToSetGameObject[i], gameObject);
                        } catch {}
                    }
                }
            }
        }
    }

    /// <summary>
    /// Assumption: component represents the same sComponent from source object
    /// </summary>
    static void VerifyComponent(Component component, SerializableComponent sComponent)
    {
        //Debug.Log("gil - ComponentAsText::VerifyComponent - testing " + component.GetType().Name + " with SerializableComponent " + sComponent.componentName);

        for (int i = 0; i < componentsDeserializableReferences.Count; i++)
        {
            if (sComponent.componentId == componentsDeserializableReferences[i].componentReferenceId)
            {
                Type type = componentsToSetComponent[i].GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
                PropertyInfo[] pinfos = type.GetProperties(flags);
                foreach (var pinfo in pinfos) {
                    if (pinfo.CanWrite && pinfo.Name == componentPropertyNames[i]) {
                        try {
                            pinfo.SetValue(componentsToSetComponent[i], component, null);
                        } catch {}
                    }
                }

                FieldInfo[] finfos = type.GetFields(flags);
                foreach (var finfo in finfos) {
                    if (finfo.Name == componentPropertyNames[i]) {
                        try {
                            finfo.SetValue(componentsToSetComponent[i], component);
                        } catch {}
                    }
                }
            }
        }
    }

    static void GetReferencesForDeserialization(GameObject gameObject, SerializableGameObject sGObject)
    {
        if (gameObject == null)
        {
            return;
        }

        //Debug.Log("gil - ComponentAsText::GetReferencesForDeserialization - Testing " + gameObject.name + " with sGObject " + sGObject.name);

        VerifyGameObject(gameObject, sGObject);

        Component[] copiedComponents = gameObject.GetComponents<Component>();
        if (copiedComponents != null)
        {
            List<SerializableComponent> sComponents = sGObject.components;

            for (int c = 0; c < sComponents.Count; c++)
            {
                for (int i = 0; i < copiedComponents.Length; i++)
                {
                    if (copiedComponents[i] != null && copiedComponents[i].GetType().Name == sComponents[c].componentName)
                    {
                        VerifyComponent(copiedComponents[i], sComponents[c]);
                        copiedComponents[i] = null;
                        break;
                    }
                }
            }
        }

        if (gameObject.transform.childCount > 0)
        {
            for (int s = 0; s < gameObject.transform.childCount; s++)
            {
                GetReferencesForDeserialization(gameObject.transform.GetChild(s).gameObject, sGObject.children[s]);
            }
        }
    }

    static bool isDuplicatedComponent(GameObject gameObject, SerializableGameObject sGObject, SerializableComponent component)
    {
        int numberOfComponents = 0;

        foreach (var copiedComponent in sGObject.components)
        {
            if (copiedComponent.componentName == component.componentName)
            {
                numberOfComponents++;
            }
        }

        return gameObject.GetComponent(component.componentName) != null && numberOfComponents == 1;

    }

    static void PasteSerializableGameObject(GameObject gameObject, SerializableGameObject sGObject)
    {
        gameObject.name = sGObject.name;

        foreach (var copiedComponent in sGObject.components)
        {
            if (copiedComponent == null) continue;

            Component comp;

            if (copiedComponent.componentName == "Transform")
            {
                comp = (Component) gameObject.transform;
            }
            else if (isDuplicatedComponent(gameObject, sGObject, copiedComponent))
            {
                comp = gameObject.GetComponent(copiedComponent.componentName);
            }
            else
            {
                comp = UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(gameObject, "Assets/Editor/Tools/CopyComponents/ComponentAsText.cs (134,21)", copiedComponent.componentName);
            }

            if (comp == null) continue;

            copiedComponent.ApplySerializedDataTo(comp);
        }

        GameObject childGObject;

        foreach (var sGChild in sGObject.children)
        {
            childGObject = new GameObject();
            childGObject.transform.parent = gameObject.transform;
            PasteSerializableGameObject(childGObject, sGChild);
        }
    }
}