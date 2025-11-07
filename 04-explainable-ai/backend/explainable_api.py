"""
HearLoveen - Explainable AI Module
===================================

این ماژول توضیحات قابل فهم برای تصمیمات AI به پزشکان ارائه می‌دهد.
از SHAP (SHapley Additive exPlanations) استفاده می‌کند.

Author: Yasser Ebrahimi Fard
Date: November 2025
"""

import numpy as np
import pandas as pd
import shap
import matplotlib.pyplot as plt
from typing import Dict, List, Tuple
import torch
import torch.nn as nn

# ===========================
# 1. PRONUNCIATION EXPLAINER
# ===========================

class ExplainablePronunciationScorer:
    """
    توضیح دهنده نمرات تلفظ با استفاده از SHAP
    """
    
    def __init__(self, model, feature_names):
        """
        Args:
            model: مدل آموزش دیده (PyTorch یا sklearn)
            feature_names: نام ویژگی‌ها (مثلاً ['MFCC1', 'MFCC2', ...])
        """
        self.model = model
        self.feature_names = feature_names
        self.explainer = None
        
    def initialize_explainer(self, background_data):
        """
        راه‌اندازی SHAP explainer با داده‌های پس‌زمینه
        
        Args:
            background_data: نمونه‌ای از داده‌های آموزشی
        """
        # برای مدل‌های PyTorch
        if isinstance(self.model, nn.Module):
            self.explainer = shap.DeepExplainer(self.model, background_data)
        else:
            # برای مدل‌های sklearn
            self.explainer = shap.KernelExplainer(
                self.model.predict, 
                background_data
            )
        print("✅ SHAP Explainer initialized")
    
    def explain_prediction(self, audio_features):
        """
        توضیح یک پیش‌بینی خاص
        
        Args:
            audio_features: ویژگی‌های صوتی (41-dim MFCC+)
            
        Returns:
            Dict حاوی توضیحات
        """
        # پیش‌بینی
        score = self.model.predict(audio_features.reshape(1, -1))[0]
        
        # محاسبه SHAP values
        shap_values = self.explainer.shap_values(audio_features.reshape(1, -1))
        
        if isinstance(shap_values, list):
            shap_values = shap_values[0]
        
        # مرتب‌سازی ویژگی‌ها بر اساس اهمیت
        feature_importance = list(zip(
            self.feature_names,
            shap_values[0],
            audio_features
        ))
        feature_importance.sort(key=lambda x: abs(x[1]), reverse=True)
        
        # تولید توضیحات قابل فهم
        explanation = self._generate_human_explanation(
            score, 
            feature_importance[:5]  # top 5 features
        )
        
        return {
            'pronunciation_score': float(score),
            'shap_values': shap_values[0].tolist(),
            'feature_importance': feature_importance,
            'human_explanation': explanation,
            'actionable_recommendations': self._generate_recommendations(
                feature_importance[:5]
            )
        }
    
    def _generate_human_explanation(
        self, 
        score: float, 
        top_features: List[Tuple]
    ) -> str:
        """
        تولید توضیح به زبان ساده
        """
        explanation = f"نمره تلفظ: {score:.1f}/100\n\n"
        
        if score >= 80:
            explanation += "✅ تلفظ خیلی خوب است!\n\n"
        elif score >= 60:
            explanation += "⚠️ تلفظ قابل قبول، اما نیاز به بهبود دارد.\n\n"
        else:
            explanation += "❌ تلفظ نیاز به تمرین بیشتری دارد.\n\n"
        
        explanation += "عوامل مؤثر اصلی:\n"
        
        for feature_name, shap_value, feature_value in top_features:
            impact = "مثبت ✅" if shap_value > 0 else "منفی ❌"
            
            # ترجمه ویژگی‌های فنی به زبان ساده
            simple_explanation = self._translate_feature(feature_name, feature_value)
            
            explanation += f"  • {simple_explanation} (تأثیر {impact})\n"
        
        return explanation
    
    def _translate_feature(self, feature_name: str, value: float) -> str:
        """
        ترجمه نام ویژگی فنی به توضیح ساده
        """
        translations = {
            'spectral_centroid': 'مرکز طیف صوت',
            'spectral_flux': 'تغییرات صدا',
            'mfcc_1': 'خصوصیت اول صدا (رنگ صدا)',
            'mfcc_2': 'خصوصیت دوم صدا',
            'pitch_mean': 'میانگین زیر و بمی صدا',
            'pitch_std': 'تغییرات زیر و بمی',
            'energy': 'انرژی صدا',
            'zcr': 'نرخ عبور از صفر',
            'formant_f1': 'فورمانت اول (موقعیت زبان)',
            'formant_f2': 'فورمانت دوم (شکل دهان)',
        }
        
        simple_name = translations.get(feature_name, feature_name)
        
        # اضافه کردن جزئیات بیشتر بر اساس مقدار
        if 'mfcc' in feature_name.lower():
            if abs(value) > 2:
                return f"{simple_name}: مقدار غیرعادی"
            else:
                return f"{simple_name}: مقدار عادی"
        elif 'pitch' in feature_name.lower():
            if value < 200:
                return f"{simple_name}: پایین (زیر انتظار برای کودک)"
            elif value > 500:
                return f"{simple_name}: بالا (بالاتر از انتظار)"
            else:
                return f"{simple_name}: در محدوده طبیعی"
        elif 'energy' in feature_name.lower():
            if value < 0.01:
                return f"{simple_name}: ضعیف (کودک ممکن است آرام صحبت کند)"
            else:
                return f"{simple_name}: مناسب"
        
        return simple_name
    
    def _generate_recommendations(self, top_features: List[Tuple]) -> List[str]:
        """
        تولید توصیه‌های عملی برای درمانگر
        """
        recommendations = []
        
        for feature_name, shap_value, feature_value in top_features:
            if shap_value < -0.5:  # تأثیر منفی قوی
                
                if 'formant' in feature_name.lower():
                    recommendations.append({
                        'issue': 'موقعیت اندام‌های گفتاری',
                        'recommendation': 'تمرینات موقعیت زبان و شکل دهان',
                        'exercises': [
                            'آینه‌ای تمرین کنید',
                            'تصاویر مدل صحیح را نشان دهید',
                            'از ابزارهای لمسی استفاده کنید'
                        ],
                        'priority': 'high'
                    })
                
                elif 'pitch' in feature_name.lower():
                    recommendations.append({
                        'issue': 'کنترل pitch (زیر و بمی)',
                        'recommendation': 'تمرینات تن و آواز',
                        'exercises': [
                            'خواندن با تغییر pitch',
                            'تقلید صداهای بالا و پایین',
                            'استفاده از نرم‌افزار visual pitch'
                        ],
                        'priority': 'medium'
                    })
                
                elif 'energy' in feature_name.lower():
                    recommendations.append({
                        'issue': 'انرژی صدا ضعیف',
                        'recommendation': 'تمرینات تنفسی و تقویت صدا',
                        'exercises': [
                            'تنفس عمیق از دیافراگم',
                            'صحبت با صدای بلندتر',
                            'تمرین projection'
                        ],
                        'priority': 'medium'
                    })
                
                elif 'spectral' in feature_name.lower():
                    recommendations.append({
                        'issue': 'وضوح صدا',
                        'recommendation': 'تمرینات articulation',
                        'exercises': [
                            'حرکات دقیق لب و زبان',
                            'تمرین هجا به هجا',
                            'slow motion speech'
                        ],
                        'priority': 'high'
                    })
        
        # اگر توصیه‌ای نبود، یک توصیه عمومی
        if not recommendations:
            recommendations.append({
                'issue': 'ادامه تمرینات فعلی',
                'recommendation': 'عملکرد خوب است، ادامه دهید',
                'exercises': [
                    'حفظ تمرینات منظم',
                    'افزایش تدریجی سختی',
                    'تمرین در موقعیت‌های مختلف'
                ],
                'priority': 'low'
            })
        
        return recommendations

# ===========================
# 2. EMOTION EXPLAINER
# ===========================

class ExplainableEmotionDetector:
    """
    توضیح تشخیص احساسات
    """
    
    def __init__(self, model):
        self.model = model
        self.emotion_labels = [
            'Happy', 'Sad', 'Angry', 'Fearful', 
            'Neutral', 'Surprised', 'Disgusted'
        ]
    
    def explain_emotion(self, audio_features, iot_data=None):
        """
        توضیح تشخیص احساسات
        
        Args:
            audio_features: ویژگی‌های صوتی
            iot_data: داده‌های IoT (ضربان قلب، ...)
            
        Returns:
            Dict حاوی توضیحات احساسی
        """
        # پیش‌بینی احساسات
        emotion_probs = self.model.predict_proba(audio_features)[0]
        predicted_emotion = self.emotion_labels[np.argmax(emotion_probs)]
        confidence = np.max(emotion_probs)
        
        # تحلیل ویژگی‌های صوتی
        prosodic_analysis = self._analyze_prosody(audio_features)
        
        # تحلیل داده‌های IoT (اگر موجود باشد)
        iot_analysis = None
        if iot_data is not None:
            iot_analysis = self._analyze_iot_data(iot_data)
        
        # تولید توضیحات
        explanation = self._generate_emotion_explanation(
            predicted_emotion,
            confidence,
            prosodic_analysis,
            iot_analysis
        )
        
        return {
            'predicted_emotion': predicted_emotion,
            'confidence': float(confidence),
            'all_probabilities': {
                label: float(prob) 
                for label, prob in zip(self.emotion_labels, emotion_probs)
            },
            'prosodic_analysis': prosodic_analysis,
            'iot_analysis': iot_analysis,
            'explanation': explanation,
            'recommendations': self._generate_emotion_recommendations(
                predicted_emotion,
                prosodic_analysis,
                iot_analysis
            )
        }
    
    def _analyze_prosody(self, features):
        """تحلیل ویژگی‌های آهنگین گفتار"""
        # فرض: ویژگی‌ها شامل pitch, energy, duration
        return {
            'pitch_level': 'high' if features[0] > 300 else 'normal',
            'energy_level': 'high' if features[1] > 0.5 else 'low',
            'speech_rate': 'fast' if features[2] < 0.1 else 'normal'
        }
    
    def _analyze_iot_data(self, iot_data):
        """تحلیل داده‌های سنسور"""
        analysis = {}
        
        if 'heart_rate' in iot_data:
            hr = iot_data['heart_rate']
            if hr > 100:
                analysis['stress_level'] = 'high'
                analysis['stress_indicator'] = '⚠️ ضربان قلب بالا'
            else:
                analysis['stress_level'] = 'normal'
                analysis['stress_indicator'] = '✅ ضربان قلب عادی'
        
        if 'noise_level' in iot_data:
            noise = iot_data['noise_level']
            if noise > 60:
                analysis['environment'] = 'noisy'
                analysis['environment_note'] = '⚠️ محیط پرسروصدا'
            else:
                analysis['environment'] = 'quiet'
                analysis['environment_note'] = '✅ محیط آرام'
        
        return analysis
    
    def _generate_emotion_explanation(
        self, 
        emotion, 
        confidence, 
        prosody, 
        iot
    ):
        """تولید توضیح احساسی"""
        
        explanation = f"احساس تشخیص داده شده: {emotion} "
        explanation += f"(اطمینان: {confidence*100:.1f}%)\n\n"
        
        # توضیح بر اساس ویژگی‌های صوتی
        explanation += "بر اساس تحلیل صدا:\n"
        explanation += f"  • سطح صدا: {prosody['pitch_level']}\n"
        explanation += f"  • انرژی: {prosody['energy_level']}\n"
        explanation += f"  • سرعت گفتار: {prosody['speech_rate']}\n\n"
        
        # اضافه کردن داده‌های IoT
        if iot:
            explanation += "دادههای حیاتی:\n"
            if 'stress_indicator' in iot:
                explanation += f"  • {iot['stress_indicator']}\n"
            if 'environment_note' in iot:
                explanation += f"  • {iot['environment_note']}\n"
        
        return explanation
    
    def _generate_emotion_recommendations(self, emotion, prosody, iot):
        """توصیه‌های بر اساس احساسات"""
        
        recommendations = []
        
        if emotion in ['Sad', 'Frustrated', 'Angry']:
            recommendations.append({
                'action': 'استراحت',
                'reason': 'کودک ممکن است خسته یا ناامید باشد',
                'suggestion': 'استراحت 10 دقیقه‌ای قبل از ادامه'
            })
        
        if iot and iot.get('stress_level') == 'high':
            recommendations.append({
                'action': 'کاهش استرس',
                'reason': 'ضربان قلب بالا نشان‌دهنده استرس است',
                'suggestion': 'تمرینات تنفسی یا بازی آرامش‌بخش'
            })
        
        if iot and iot.get('environment') == 'noisy':
            recommendations.append({
                'action': 'بهبود محیط',
                'reason': 'سروصدای محیط تمرکز را کاهش می‌دهد',
                'suggestion': 'به محیط آرام‌تری بروید یا سروصدا را کم کنید'
            })
        
        if emotion == 'Happy' and prosody['energy_level'] == 'high':
            recommendations.append({
                'action': 'ادامه تمرین',
                'reason': 'کودک انگیزه دارد و آماده یادگیری است',
                'suggestion': 'این زمان مناسبی برای تمرینات چالش‌برانگیز است'
            })
        
        return recommendations

# ===========================
# 3. DASHBOARD GENERATOR
# ===========================

def generate_therapist_dashboard(child_id, date_range):
    """
    تولید داشبورد برای درمانگر با توضیحات کامل
    """
    # این تابع فرضی است و باید به backend متصل شود
    
    dashboard_data = {
        'child_info': {
            'id': child_id,
            'name': '[ANONYMIZED]',
            'age': 8,
            'hearing_loss': 'Moderate'
        },
        'summary': {
            'total_sessions': 45,
            'avg_pronunciation_score': 72.5,
            'improvement_rate': '+15%',
            'current_emotion': 'Happy',
            'engagement_level': 'High'
        },
        'explanations': [],
        'recommendations': []
    }
    
    return dashboard_data

# ===========================
# 4. API ENDPOINT
# ===========================

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI(title="HearLoveen Explainable AI API")

class AudioAnalysisRequest(BaseModel):
    audio_features: List[float]
    phoneme: str
    child_id: str

@app.post("/api/explain/pronunciation")
async def explain_pronunciation(request: AudioAnalysisRequest):
    """
    API endpoint برای توضیح نمره تلفظ
    """
    try:
        # Load model (در production از cache استفاده کنید)
        # model = load_pronunciation_model()
        
        # Initialize explainer
        # explainer = ExplainablePronunciationScorer(model, feature_names)
        
        # Explain
        # result = explainer.explain_prediction(np.array(request.audio_features))
        
        # برای demo:
        result = {
            'pronunciation_score': 75.0,
            'human_explanation': 'تلفظ قابل قبول، اما نیاز به بهبود در موقعیت زبان دارد.',
            'actionable_recommendations': [
                {
                    'issue': 'موقعیت زبان',
                    'recommendation': 'تمرینات آینه‌ای',
                    'priority': 'high'
                }
            ]
        }
        
        return result
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/dashboard/therapist/{child_id}")
async def get_therapist_dashboard(child_id: str):
    """
    داشبورد کامل برای درمانگر
    """
    try:
        dashboard = generate_therapist_dashboard(child_id, date_range='last_30_days')
        return dashboard
    
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

# ===========================
# MAIN
# ===========================

if __name__ == "__main__":
    print("🚀 HearLoveen Explainable AI Module")
    print("="*60)
    print("✅ SHAP-based explanations")
    print("✅ Human-readable output")
    print("✅ Actionable recommendations")
    print("✅ Therapist dashboard")
    print("="*60)
    
    # Run API server
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
