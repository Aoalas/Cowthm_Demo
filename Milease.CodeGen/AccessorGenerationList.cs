#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

namespace Milease.CodeGen
{
    public static class AccessorGenerationList
    {
        // Put the member for animation code accessor generation here
        public static IEnumerable<LambdaExpression> GetGenerateMembers()
        {
            return new []
            {
                #region TextMeshPro

                /**new Expression<Func<TMP_Text, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),
                new Expression<Func<TMP_Text, float>>[]
                {
                    x => x.characterSpacing,
                    x => x.wordSpacing,
                    x => x.lineSpacing,
                    x => x.fontSize,
                    x => x.alpha
                }.Cast<LambdaExpression>(),
                new Expression<Func<TMP_Text, Vector4>>[]
                {
                    x => x.margin
                }.Cast<LambdaExpression>(),
                new Expression<Func<TMP_Text, string>>[]
                {
                    x => x.text
                }.Cast<LambdaExpression>(),**/

                #endregion

                #region Transform

                new Expression<Func<Transform, Vector3>>[]
                {
                    x => x.position,
                    x => x.localPosition,
                    x => x.localScale,
                    x => x.eulerAngles,
                    x => x.localEulerAngles
                }.Cast<LambdaExpression>(),

                #endregion

                #region RectTransform

                new Expression<Func<RectTransform, Vector2>>[]
                {
                    x => x.anchoredPosition,
                    x => x.sizeDelta,
                    x => x.anchorMin,
                    x => x.anchorMax,
                    x => x.pivot
                }.Cast<LambdaExpression>(),
                new Expression<Func<RectTransform, Vector3>>[]
                {
                    x => x.position,
                    x => x.localPosition,
                    x => x.localScale,
                    x => x.eulerAngles,
                    x => x.localEulerAngles,
                    x => x.anchoredPosition3D
                }.Cast<LambdaExpression>(),

                #endregion

                #region SpriteRenderer

                new Expression<Func<SpriteRenderer, Vector2>>[]
                {
                    x => x.size
                }.Cast<LambdaExpression>(),
                new Expression<Func<SpriteRenderer, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),
                new Expression<Func<SpriteRenderer, int>>[]
                {
                    x => x.sortingOrder
                }.Cast<LambdaExpression>(),

                #endregion

                #region Rigidbody (3D)

                new Expression<Func<Rigidbody, Vector3>>[]
                {
                    x => x.velocity,
                    x => x.angularVelocity
                }.Cast<LambdaExpression>(),

                #endregion

                #region Rigidbody2D

                new Expression<Func<Rigidbody2D, Vector2>>[]
                {
                    x => x.velocity
                }.Cast<LambdaExpression>(),
                new Expression<Func<Rigidbody2D, float>>[]
                {
                    x => x.angularVelocity
                }.Cast<LambdaExpression>(),

                #endregion

                #region AudioSource

                new Expression<Func<AudioSource, float>>[]
                {
                    x => x.volume,
                    x => x.pitch,
                    x => x.spatialBlend,
                    x => x.panStereo,
                    x => x.dopplerLevel
                }.Cast<LambdaExpression>(),

                #endregion

                #region UI Components

                // CanvasGroup
                new Expression<Func<CanvasGroup, float>>[]
                {
                    x => x.alpha
                }.Cast<LambdaExpression>(),

                // Graphic
                new Expression<Func<Graphic, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),

                // UI Image
                new Expression<Func<Image, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),
                new Expression<Func<Image, float>>[]
                {
                    x => x.fillAmount
                }.Cast<LambdaExpression>(),

                // UI Text
                new Expression<Func<Text, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),
                new Expression<Func<Text, string>>[]
                {
                    x => x.text
                }.Cast<LambdaExpression>(),
                
                // Layout Element
                new Expression<Func<LayoutElement, float>>[]
                {
                    x => x.flexibleWidth,
                    x => x.flexibleHeight,
                    x => x.minWidth,
                    x => x.minHeight,
                    x => x.preferredWidth,
                    x => x.preferredHeight
                }.Cast<LambdaExpression>(),
                
                new Expression<Func<HorizontalLayoutGroup, float>>[]
                {
                    x => x.spacing,
                }.Cast<LambdaExpression>(),
                
                new Expression<Func<VerticalLayoutGroup, float>>[]
                {
                    x => x.spacing,
                }.Cast<LambdaExpression>(),
                
                // Outline
                new Expression<Func<Outline, Color>>[]
                {
                    x => x.effectColor
                }.Cast<LambdaExpression>(),
                new Expression<Func<Outline, Vector2>>[]
                {
                    x => x.effectDistance
                }.Cast<LambdaExpression>(),

                // Scroll Rect
                new Expression<Func<ScrollRect, float>>[]
                {
                    x => x.horizontalNormalizedPosition,
                    x => x.verticalNormalizedPosition
                }.Cast<LambdaExpression>(),

                // Slider
                new Expression<Func<Slider, float>>[]
                {
                    x => x.value
                }.Cast<LambdaExpression>(),

                #endregion

                #region Material

                new Expression<Func<Material, Color>>[]
                {
                    x => x.color
                }.Cast<LambdaExpression>(),

                #endregion
            }.SelectMany(x => x);
        }
    }
}
#endif
