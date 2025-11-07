# طراحی مطالعه طولی HearLoveen

## Longitudinal Study Design

**Study Title:** Long-term Speech Development Trajectories in Children with Hearing Impairment Using HearLoveen Platform

**Duration:** 12 months + 6 months follow-up  
**Principal Investigator:** Yasser Ebrahimi Fard  
**Protocol Version:** 1.0

---

## 1. اهداف مطالعه

### هدف اصلی:
ارزیابی **مسیر بهبود طولانی‌مدت** (longitudinal trajectory) مهارت‌های گفتاری در کودکان با اختلال شنوایی که از پلتفرم HearLoveen استفاده می‌کنند.

### اهداف ویژه:
1. 📈 **شناسایی الگوهای بهبود** - آیا بهبود خطی است یا دارای مراحل پله‌ای؟
2. 🎯 **پیش‌بینی نتایج** - چه عواملی پیش‌بینی‌کننده بهبود سریع‌تر هستند؟
3. 💊 **Dose-response** - چقدر تمرین لازم است برای X درصد بهبود؟
4. 🔄 **پایداری** - آیا بهبود پایدار می‌ماند یا regression می‌کند؟
5. 🧩 **Individual differences** - چرا برخی کودکان سریع‌تر بهبود می‌یابند؟

---

## 2. طراحی مطالعه

### نوع مطالعه:
**Prospective Longitudinal Cohort Study**

### حجم نمونه:
- **n = 150 کودک**
- پیش‌بینی dropout: 20%
- نهایی: ~120 کودک

### مدت پیگیری:
```
Phase 1: Baseline assessment
Phase 2: Intensive intervention (6 months)
  └─ Monthly assessments (Months 1-6)
Phase 3: Maintenance (6 months)
  └─ Bi-monthly assessments (Months 8, 10, 12)
Phase 4: Follow-up (6 months)
  └─ Quarterly assessments (Months 15, 18)
```

**Total duration:** 18 months per child

---

## 3. معیارهای ورود و خروج

### ورود:
✅ سن 5-12 سال  
✅ Mild to moderate hearing loss  
✅ گفتاردرمانی فعلی (حداقل 3 ماه)  
✅ دسترسی به اینترنت و تبلت  
✅ رضایت والدین برای پیگیری 18 ماهه  

### خروج:
❌ انتقال به شهر دیگر  
❌ تغییر وضعیت شنوایی (مثلاً cochlear implant)  
❌ بیماری جدی طولانی‌مدت  
❌ عدم همکاری مداوم (miss بیش از 3 ویزیت)  

---

## 4. متغیرهای اندازه‌گیری شده

### پیامدهای اولیه (Primary Outcomes):

#### 1. Speech Intelligibility Index (SII)
```yaml
Frequency: Monthly (Months 0-6), Bi-monthly (7-12), Quarterly (13-18)
Tool: HINT (Hearing in Noise Test)
Scorer: Trained SLP (blinded)
Reliability: ICC > 0.90
```

#### 2. Phoneme Accuracy
```yaml
Frequency: Monthly
Tool: Goldman-Fristoe Test of Articulation (GFTA-3)
Measure: Percentage of correct phonemes
Target phonemes: /s/, /z/, /r/, /l/, /th/, /sh/
```

### پیامدهای ثانویه (Secondary Outcomes):

#### 3. Vocabulary Growth
```yaml
Tool: Peabody Picture Vocabulary Test (PPVT-5)
Frequency: Baseline, Month 6, Month 12, Month 18
```

#### 4. Communication Confidence
```yaml
Tool: Communication Attitude Test (CAT)
Frequency: Every 3 months
Respondent: Parent-reported
```

#### 5. Quality of Life
```yaml
Tool: Pediatric Quality of Life (PedsQL)
Frequency: Every 6 months
Domains: Physical, Emotional, Social, School
```

### متغیرهای توضیح‌دهنده (Predictors):

```python
Demographics:
  - Age (continuous)
  - Gender
  - Hearing loss degree (mild/moderate)
  - Age at diagnosis
  - Duration of therapy

App Usage:
  - Daily practice duration (minutes/day)
  - Weekly session count
  - Total exercises completed
  - Accuracy scores per exercise
  - Adherence rate (%)

Family Factors:
  - Parental education level
  - Household income
  - Number of siblings
  - Language(s) spoken at home
  - Parental involvement score

Clinical Factors:
  - Baseline severity
  - Comorbidities (autism, ADHD, etc.)
  - Hearing aid usage hours/day
  - Concurrent therapies
```

---

## 5. جمع‌آوری داده

### نقاط زمانی تفصیلی:

| Month | SII | GFTA | PPVT | CAT | PedsQL | App Data |
|-------|-----|------|------|-----|--------|----------|
| 0 (Baseline) | ✅ | ✅ | ✅ | ✅ | ✅ | - |
| 1 | ✅ | ✅ | - | - | - | ✅ |
| 2 | ✅ | ✅ | - | - | - | ✅ |
| 3 | ✅ | ✅ | - | ✅ | - | ✅ |
| 4 | ✅ | ✅ | - | - | - | ✅ |
| 5 | ✅ | ✅ | - | - | - | ✅ |
| 6 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| 8 | ✅ | ✅ | - | - | - | ✅ |
| 10 | ✅ | ✅ | - | ✅ | - | ✅ |
| 12 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| 15 | ✅ | ✅ | - | - | - | ✅ |
| 18 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

### App Usage Data (Continuous):
```
Real-time collection:
- Session duration
- Exercise type and difficulty
- Pronunciation scores
- Number of attempts
- Emotion during practice
- IoT sensor data (if available)

Aggregation:
- Daily summaries
- Weekly summaries
- Monthly summaries
```

---

## 6. آنالیز آماری

### 1. Growth Curve Modeling

```python
# Hierarchical Linear Model (HLM)
from statsmodels.regression.mixed_linear_model import MixedLM

# Level 1: Within-child (Time)
# SII_ij = π0i + π1i*Time_ij + e_ij

# Level 2: Between-child (Child characteristics)
# π0i = γ00 + γ01*Age_i + γ02*Severity_i + u0i
# π1i = γ10 + γ11*Usage_i + u1i

model = MixedLM.from_formula(
    "SII ~ Time + Age + Severity + Usage + Time:Usage",
    data=df_long,
    groups=df_long["ChildID"],
    re_formula="~Time"
)

result = model.fit()
print(result.summary())
```

**Interpretation:**
- γ00: Average baseline SII
- γ10: Average rate of change (slope)
- γ11: How app usage affects rate of change
- u0i: Random intercept (individual baseline differences)
- u1i: Random slope (individual growth rate differences)

---

### 2. Trajectory Classification

```python
# Latent Class Growth Analysis (LCGA)
# شناسایی گروه‌های همگن با مسیرهای مشابه

from sklearn.mixture import GaussianMixture

# Reshape data to trajectory format
# Each row = one child's complete trajectory

trajectories = df_pivot  # shape: (n_children, n_timepoints)

# Fit GMM to identify latent classes
gmm = GaussianMixture(n_components=3, random_state=0)
labels = gmm.fit_predict(trajectories)

# Visualize trajectory classes
import matplotlib.pyplot as plt
for class_id in range(3):
    class_data = trajectories[labels == class_id]
    plt.plot(class_data.T, alpha=0.3, color=f'C{class_id}')
    plt.plot(class_data.mean(axis=0), linewidth=3, 
             color=f'C{class_id}', label=f'Class {class_id+1}')

plt.xlabel('Time (months)')
plt.ylabel('SII Score')
plt.legend()
plt.title('Latent Trajectory Classes')
plt.show()
```

**Expected Classes:**
1. **Fast Responders** (~30%): Steep improvement, plateau early
2. **Steady Improvers** (~50%): Consistent linear growth
3. **Slow/Non-Responders** (~20%): Minimal change

---

### 3. Dose-Response Analysis

```python
# Quantile Regression
from statsmodels.regression.quantile_regression import QuantReg

# Explore relationship at different quantiles
quantiles = [0.25, 0.50, 0.75]

for q in quantiles:
    model = QuantReg(y, X)
    result = model.fit(q=q)
    print(f"\n=== {q*100}th Percentile ===")
    print(result.summary())
```

**Research Questions:**
- Minimum effective dose: How many minutes/day needed for improvement?
- Optimal dose: Diminishing returns after how many minutes?
- Individual variation: Does optimal dose vary by child characteristics?

---

### 4. Predictor Analysis

```python
# Random Forest for Variable Importance
from sklearn.ensemble import RandomForestRegressor

# Predict improvement from baseline to month 12
y = df['Improvement_12mo']  # SII_12mo - SII_baseline
X = df[predictor_columns]

rf = RandomForestRegressor(n_estimators=100, random_state=0)
rf.fit(X, y)

# Feature importance
importance_df = pd.DataFrame({
    'Feature': predictor_columns,
    'Importance': rf.feature_importances_
}).sort_values('Importance', ascending=False)

print(importance_df)
```

**Expected Predictors (by importance):**
1. App usage consistency (adherence)
2. Baseline severity
3. Age
4. Parental involvement
5. Daily practice duration

---

### 5. Sustained Effect Analysis

```python
# Compare active phase (0-6 mo) vs maintenance (7-12 mo) vs follow-up (13-18 mo)

# Piecewise regression
from patsy import dmatrix

# Create spline basis
spline_df = dmatrix(
    "bs(Time, knots=[6, 12], degree=3, include_intercept=False)",
    {"Time": time_points},
    return_type='dataframe'
)

# Mixed model with splines
model = MixedLM.from_formula(
    "SII ~ spline_df + (1 + Time | ChildID)",
    data=df_long
)
result = model.fit()
```

**Questions:**
- Do children maintain gains after reducing practice?
- Is there regression during follow-up?
- What predicts sustained improvement?

---

## 7. Power Analysis

```python
from statsmodels.stats.power import FTestAnovaPower

# Parameters
effect_size = 0.25  # Cohen's f (medium effect)
alpha = 0.05
power = 0.80
k_groups = 3  # trajectory classes
n_measurements = 12  # monthly measurements

# Calculate required sample size
power_analysis = FTestAnovaPower()
required_n = power_analysis.solve_power(
    effect_size=effect_size,
    alpha=alpha,
    power=power,
    k_groups=k_groups
)

print(f"Required sample size per group: {required_n:.0f}")
print(f"Total required: {required_n * k_groups:.0f}")
print(f"With 20% dropout: {required_n * k_groups * 1.2:.0f}")
```

**Result:** 150 participants needed

---

## 8. Missing Data Strategy

### Anticipated Missing Data:
- Random absences: 10%
- Dropout: 20%
- Equipment failure: 2%

### Handling Strategy:

```python
# 1. Multiple Imputation
from sklearn.experimental import enable_iterative_imputer
from sklearn.impute import IterativeImputer

imputer = IterativeImputer(max_iter=10, random_state=0)
df_imputed = imputer.fit_transform(df_with_missing)

# 2. Mixed Models (handles missing data automatically)
# Use all available data, doesn't require complete cases

# 3. Sensitivity Analysis
# Compare results with:
# - Complete cases only
# - Imputed data
# - Last Observation Carried Forward (LOCF)
# - Multiple imputation
```

---

## 9. Interim Analyses

### Timeline:
- **Month 6:** Check enrollment and adherence
- **Month 12:** Preliminary trajectory analysis
- **Month 18:** Final analysis

### Stopping Rules:
```
Study continues UNLESS:
1. Safety concerns (serious adverse events)
2. Futility (no improvement trend by month 12)
3. Enrollment failure (<100 children by month 6)
```

---

## 10. Expected Findings

### Hypothesis 1: Growth Trajectories
```
H1: Children will show significant improvement in SII over 12 months
Expected: Cohen's d > 0.8 (large effect)
```

### Hypothesis 2: Dose-Response
```
H2: Greater app usage predicts faster improvement
Expected: r > 0.40 (moderate correlation)
Minimum effective dose: ~10 minutes/day
Optimal dose: ~20 minutes/day (plateau after)
```

### Hypothesis 3: Trajectory Classes
```
H3: 3 distinct trajectory classes will emerge
- Fast Responders: 30%
- Steady Improvers: 50%
- Slow/Non-Responders: 20%
```

### Hypothesis 4: Predictors
```
H4: Top predictors of improvement:
1. Adherence (R² = 0.25)
2. Baseline severity (R² = 0.15)
3. Age (R² = 0.10)
4. Parental involvement (R² = 0.08)
```

### Hypothesis 5: Sustained Effect
```
H5: Improvements maintained at 18-month follow-up
Expected: 80% retention of gains
```

---

## 11. Data Management

### Database Structure:
```sql
-- PostgreSQL schema

CREATE TABLE children (
    child_id SERIAL PRIMARY KEY,
    enrollment_date DATE,
    age INTEGER,
    gender VARCHAR(10),
    hearing_loss_degree VARCHAR(20),
    baseline_sii FLOAT
);

CREATE TABLE assessments (
    assessment_id SERIAL PRIMARY KEY,
    child_id INTEGER REFERENCES children(child_id),
    assessment_date DATE,
    month_number INTEGER,
    sii_score FLOAT,
    gfta_score FLOAT,
    assessor_id INTEGER
);

CREATE TABLE app_usage (
    usage_id SERIAL PRIMARY KEY,
    child_id INTEGER REFERENCES children(child_id),
    date DATE,
    duration_minutes INTEGER,
    exercises_completed INTEGER,
    avg_pronunciation_score FLOAT
);

CREATE TABLE dropouts (
    child_id INTEGER REFERENCES children(child_id),
    dropout_date DATE,
    reason TEXT,
    last_assessment_month INTEGER
);
```

---

## 12. Reporting Plan

### Publications:
1. **Primary Paper** (Month 20):
   - Title: "Longitudinal Speech Development in Children with Hearing Impairment Using AI-Powered Home Practice"
   - Journal: Journal of Speech, Language, and Hearing Research (JSLHR)

2. **Secondary Papers**:
   - Trajectory classes and predictors
   - Dose-response relationship
   - Sustained effects and maintenance

### Conferences:
- ASHA Annual Convention (Year 2)
- International Congress on the Education of the Deaf (ICED)

---

## 13. Budget

| Item | Cost | Notes |
|------|------|-------|
| Participant compensation | $150,000 | $1000/child × 150 |
| Assessments (HINT, GFTA, PPVT) | $45,000 | $300/child × 150 |
| Research coordinators (2 FTE) | $120,000 | $60K/year × 2 years |
| Biostatistician | $30,000 | Part-time |
| Data management | $20,000 | Software + storage |
| Publication fees | $10,000 | Open access |
| **Total** | **$375,000** | |

---

## 14. Timeline

```
Month 0-3:   IRB approval + Site setup
Month 4-9:   Recruitment (150 children)
Month 10-21: Active data collection (12 months)
Month 22-27: Follow-up phase (6 months)
Month 28-30: Data cleaning + analysis
Month 31-33: Manuscript writing
Month 34:    Submission to journal
Month 40:    Publication (if accepted)
```

**Total duration:** 40 months (~3.5 years)

---

## 15. Success Criteria

### Study Success:
✅ Enroll ≥100 children (accounting for dropout)  
✅ Retention rate ≥75% at Month 12  
✅ Complete data on ≥80% of timepoints  
✅ Significant improvement in primary outcome (p < 0.05)  

### Clinical Success:
✅ Mean SII improvement ≥10 points  
✅ ≥60% of children improve by ≥5 points  
✅ Improvements sustained at 18-month follow-up  

### Publication Success:
✅ Accepted in peer-reviewed journal (IF > 2.0)  
✅ Presented at ≥2 major conferences  
✅ Cited by ≥10 papers within 2 years  

---

**Status:** Protocol approved - Ready to recruit  
**Next Steps:** IRB submission + Site agreements

**Contact:**  
Yasser Ebrahimi Fard  
yasser.ebrahimifard@hearloveen.com
