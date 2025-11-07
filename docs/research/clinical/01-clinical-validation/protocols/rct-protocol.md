# پروتکل مطالعه بالینی RCT - HearLoveen

## 1. اطلاعات کلی مطالعه

### عنوان
**"اثربخشی پلتفرم HearLoveen در بهبود مهارت‌های گفتاری کودکان دارای اختلال شنوایی: یک مطالعه کارآزمایی بالینی تصادفی‌شده"**

### نوع مطالعه
- Randomized Controlled Trial (RCT)
- Double-blind (کور دوگانه)
- Parallel-group design

### محقق اصلی
Yasser Ebrahimi Fard  
Department of AI & Signal Processing  
Contact: yasser.ebrahimifard@hearloveen.com

### مدت مطالعه
- Duration: 6 ماه
- Follow-up: 3 ماه اضافی
- Total: 9 ماه

---

## 2. اهداف مطالعه

### هدف اصلی (Primary Objective)
ارزیابی تأثیر استفاده از پلتفرم HearLoveen (همراه با گفتاردرمانی سنتی) در مقایسه با گفتاردرمانی سنتی تنها، بر بهبود **Speech Intelligibility** در کودکان 5-12 ساله با اختلال شنوایی.

### اهداف ثانویه (Secondary Objectives)
1. ارزیابی تأثیر بر **Phoneme Accuracy**
2. ارزیابی رضایت والدین
3. ارزیابی پذیرش توسط درمانگران
4. ارزیابی میزان تمرین در خانه
5. ارزیابی انگیزه و اشتیاق کودک

---

## 3. معیارهای ورود و خروج

### معیارهای ورود (Inclusion Criteria)
✅ کودکان 5-12 سال  
✅ تشخیص رسمی اختلال شنوایی (mild to moderate hearing loss)  
✅ دریافت گفتاردرمانی (حداقل 3 ماه)  
✅ دسترسی به اسمارت‌فون یا تبلت  
✅ رضایت والدین و کودک  
✅ زبان مادری: انگلیسی  

### معیارهای خروج (Exclusion Criteria)
❌ اختلالات رشدی شدید همزمان (Autism Spectrum Disorder)  
❌ اختلالات شناختی شدید  
❌ عدم همکاری در جلسات  
❌ انتقال به شهر دیگر طی دوره مطالعه  

---

## 4. طراحی مطالعه

### حجم نمونه (Sample Size)
- **Target:** 200 participant
- **Group 1 (Control):** 100 کودک - گفتاردرمانی سنتی
- **Group 2 (Treatment):** 100 کودک - گفتاردرمانی + HearLoveen

### محاسبه حجم نمونه
```
Power Analysis:
- Effect Size (Cohen's d): 0.4 (medium)
- Power (1-β): 0.80
- Significance (α): 0.05
- Dropout rate: 20%

Required per group: 100
Total required: 200
```

### تصادفی‌سازی (Randomization)
```python
# استراتژی تصادفی‌سازی
Stratified Randomization:
- Strata 1: سن (5-8 vs 9-12)
- Strata 2: درجه شنوایی (mild vs moderate)
- Strata 3: جنسیت (پسر vs دختر)

Block size: 4 (AABB, ABAB, ABBA, BAAB)
```

---

## 5. مداخله (Intervention)

### گروه کنترل (Control Group)
- گفتاردرمانی سنتی: 2 جلسه/هفته × 45 دقیقه
- تمرینات خانگی با کاغذ
- بدون استفاده از اپلیکیشن

### گروه درمان (Treatment Group)  
- گفتاردرمانی سنتی: 2 جلسه/هفته × 45 دقیقه
- **+ استفاده از HearLoveen**: حداقل 15 دقیقه/روز
- بازخورد خودکار AI
- گزارش‌های پیشرفت برای والدین

---

## 6. پیامدهای اولیه و ثانویه

### پیامد اولیه (Primary Outcome)
**Speech Intelligibility Index (SII)**
- ابزار: HINT (Hearing in Noise Test)
- زمان: Baseline, Week 12, Week 24
- اندازه‌گیری: درصد کلمات قابل فهم در نویز

### پیامدهای ثانویه (Secondary Outcomes)

#### 1. Phoneme Accuracy
```
Test: Goldman-Fristoe Test of Articulation
Measure: درصد تلفظ صحیح فونم‌ها
Time points: Baseline, Week 6, Week 12, Week 18, Week 24
```

#### 2. Parent Satisfaction
```
Tool: Custom Parent Satisfaction Questionnaire (5-point Likert)
Items: 15 سوال
Time points: Week 12, Week 24
```

#### 3. Practice Adherence
```
Measure: تعداد جلسات تمرین در هفته
Data source: App logs (Treatment) / Parent diary (Control)
Time points: Weekly
```

#### 4. Child Motivation
```
Tool: Intrinsic Motivation Inventory (IMI) - Adapted for children
Items: 10 سوال با تصاویر
Time points: Week 12, Week 24
```

---

## 7. جمع‌آوری داده

### نقاط زمانی (Time Points)

| Visit | Week | Assessments |
|-------|------|-------------|
| **Screening** | -2 | رضایت‌نامه، معیارهای ورود/خروج |
| **Baseline** | 0 | HINT, GFTA, Demographics |
| **V1** | 6 | GFTA, Adherence check |
| **V2** | 12 | HINT, GFTA, Parent Survey, IMI |
| **V3** | 18 | GFTA, Adherence check |
| **V4 (Primary)** | 24 | HINT, GFTA, Parent Survey, IMI |
| **Follow-up** | 36 | HINT, Parent Survey |

### داده‌های جمع‌آوری شده

```yaml
Demographic Data:
  - Age, Gender, Ethnicity
  - Hearing loss degree
  - Duration of therapy
  - Parental education level
  - Household income (optional)

Clinical Data:
  - HINT scores
  - GFTA scores
  - Audiometry results
  - Therapist notes

App Usage Data (Treatment group only):
  - Daily practice duration
  - Number of exercises completed
  - Pronunciation scores
  - Emotion detection data
  - Device type and OS
```

---

## 8. آنالیز آماری

### Primary Analysis

**Hypothesis:**
```
H₀: μ_treatment - μ_control = 0
H₁: μ_treatment - μ_control > 0

where μ = mean change in SII from baseline to week 24
```

**Statistical Test:**
```python
# Independent samples t-test (or Mann-Whitney if non-normal)
from scipy import stats

# Check normality
shapiro_control = stats.shapiro(control_changes)
shapiro_treatment = stats.shapiro(treatment_changes)

if shapiro_control.pvalue > 0.05 and shapiro_treatment.pvalue > 0.05:
    # Parametric test
    t_stat, p_value = stats.ttest_ind(treatment_changes, control_changes)
else:
    # Non-parametric test
    u_stat, p_value = stats.mannwhitneyu(treatment_changes, control_changes)

# Effect size (Cohen's d)
pooled_std = np.sqrt((std_treatment**2 + std_control**2) / 2)
cohens_d = (mean_treatment - mean_control) / pooled_std

print(f"P-value: {p_value:.4f}")
print(f"Cohen's d: {cohens_d:.2f}")
```

### Secondary Analyses

#### 1. Repeated Measures ANOVA
```python
# برای آنالیز روند زمانی
import statsmodels.api as sm
from statsmodels.formula.api import mixedlm

# Mixed-effects model
model = mixedlm("SII ~ Time * Group", data, groups=data["ChildID"])
result = model.fit()
print(result.summary())
```

#### 2. Dose-Response Analysis
```python
# رابطه بین میزان استفاده و بهبود
from sklearn.linear_model import LinearRegression

X = daily_usage_minutes.reshape(-1, 1)
y = improvement_scores

model = LinearRegression()
model.fit(X, y)

print(f"Coefficient: {model.coef_[0]:.3f}")
print(f"R²: {model.score(X, y):.3f}")
```

#### 3. Subgroup Analysis
```
Subgroups:
- Age: 5-8 vs 9-12
- Hearing loss: Mild vs Moderate
- Gender: Boys vs Girls
- Baseline severity: Low vs High

Test for interaction: Group × Subgroup
```

### Missing Data
```python
# Multiple Imputation
from sklearn.experimental import enable_iterative_imputer
from sklearn.impute import IterativeImputer

imputer = IterativeImputer(max_iter=10, random_state=0)
data_imputed = imputer.fit_transform(data_with_missing)

# Run analysis on each imputed dataset
# Pool results using Rubin's rules
```

---

## 9. ملاحظات اخلاقی

### کمیته اخلاق (IRB Approval)
- ✅ ارسال به IRB: 4 هفته قبل از شروع
- ✅ تأیید IRB: قبل از ثبت‌نام اولین شرکت‌کننده
- ✅ شماره پروتکل: HRV-2025-001

### رضایت‌نامه (Informed Consent)
```
Components:
1. Purpose of study
2. Procedures and duration
3. Risks and benefits
4. Confidentiality
5. Voluntary participation
6. Right to withdraw
7. Contact information
8. Child assent (for ages 7+)
```

### حریم خصوصی داده
- ✅ Anonymization: هر کودک یک ID منحصر به فرد
- ✅ Encryption: داده‌های شخصی رمزگذاری شده (AES-256)
- ✅ Access control: فقط محققین مجاز
- ✅ Data retention: 5 سال پس از انتشار نتایج
- ✅ GDPR compliance: حق حذف، دسترسی به داده

---

## 10. کنترل کیفیت

### Training
- تمام ارزیابان HINT و GFTA: آموزش 4 ساعته
- Inter-rater reliability: ICC > 0.85

### Monitoring
- Data quality checks: هر 2 هفته
- Site visits: ماهانه
- Adverse events: گزارش فوری

### Stopping Rules
```
Study will be stopped if:
1. Serious adverse events related to intervention
2. Interim analysis shows futility (p > 0.80)
3. Overwhelming benefit (p < 0.001 at interim)
```

---

## 11. بودجه تخمینی

| Item | Cost | Quantity | Total |
|------|------|----------|-------|
| Participant compensation | $50/visit | 200×7 visits | $70,000 |
| Assessments (HINT, GFTA) | $100/child | 200 | $20,000 |
| Research staff | $50K/year | 2 staff × 1 year | $100,000 |
| IRB fees | $5,000 | 1 | $5,000 |
| Data management | $10,000 | 1 | $10,000 |
| Statistical analysis | $15,000 | 1 | $15,000 |
| **Total** | | | **$220,000** |

---

## 12. انتشار نتایج

### Publications
- Primary paper: Submit to **JSLHR** (Journal of Speech, Language, and Hearing Research)
- Secondary papers: Subgroup analyses
- Conference: ASHA Annual Convention 2026

### Clinical Trial Registration
- ✅ Register on ClinicalTrials.gov before enrollment
- ✅ Update results within 12 months of completion

---

## 13. تماس

**Principal Investigator:**  
Yasser Ebrahimi Fard  
Email: yasser.ebrahimifard@hearloveen.com  
Phone: [Your Number]

**IRB Contact:**  
[Institution] IRB Office  
Email: irb@institution.edu  
Phone: [IRB Number]

---

## تأیید پروتکل

| Role | Name | Signature | Date |
|------|------|-----------|------|
| PI | Yasser Ebrahimi Fard | _________ | _____ |
| Biostatistician | _________ | _________ | _____ |
| Clinical Advisor | _________ | _________ | _____ |
| IRB Chair | _________ | _________ | _____ |

---

**Protocol Version:** 1.0  
**Date:** November 7, 2025  
**Status:** Draft - Awaiting IRB Approval
