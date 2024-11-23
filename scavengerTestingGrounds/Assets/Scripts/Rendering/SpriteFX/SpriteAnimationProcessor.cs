using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimationProcessor : MonoBehaviour
{
    public enum AnimationAxis {  Rows, Columns }
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private string rowProperty = "_CurrRow", colProperty = "_CurrCol";

    [SerializeField] private AnimationAxis axis; //determined if animations are horizontal or vertical
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField] private int animationIndex = 0; //lets us select the row if the orientation is horizontal or the column if the orientation is vertical

    private void Update()
    {
        // Select shadeer property names
        string clipKey, frameKey;
        if (axis == AnimationAxis.Rows)
        {
            clipKey = rowProperty;
            frameKey = colProperty;
        } else {
            clipKey = rowProperty;
            frameKey = colProperty; //checks spritesheet orientation and assings the row and column values accordingly
                }

        //Animate
        int frame = (int)(Time.deltaTime * animationSpeed);
        meshRenderer.material.SetFloat(clipKey, animationIndex);
        meshRenderer.material.SetFloat(frameKey, frame);

    }

}
