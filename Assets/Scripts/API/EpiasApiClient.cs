using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EpiasApiClient : MonoBehaviour
{
    public IEnumerator GetData(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;

                Debug.Log("EPİAŞ Response:");
                Debug.Log(json);

                GenerationData data =
                    JsonUtility.FromJson<GenerationData>(json);

                Debug.Log("Date: " + data.date);
                Debug.Log("Total Generation: " +
                          data.totalGeneration + " MW");
            }
            else
            {
                Debug.LogError("EPİAŞ Request Failed:");
                Debug.LogError(request.error);
            }
        }
    }
}