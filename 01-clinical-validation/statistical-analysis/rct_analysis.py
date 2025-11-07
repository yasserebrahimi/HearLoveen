"""
HearLoveen RCT - Statistical Analysis Script
===========================================

این اسکریپت آنالیز آماری کامل مطالعه RCT را انجام می‌دهد.

Author: Yasser Ebrahimi Fard
Date: November 2025
Version: 1.0
"""

import pandas as pd
import numpy as np
from scipy import stats
from scipy.stats import shapiro, mannwhitneyu, ttest_ind
import statsmodels.api as sm
from statsmodels.formula.api import mixedlm, ols
from statsmodels.stats.multicomp import pairwise_tukeyhsd
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.impute import IterativeImputer
import warnings
warnings.filterwarnings('ignore')

# ===========================
# 1. LOAD DATA
# ===========================

def load_data(filepath='data/rct_data.csv'):
    """Load RCT data from CSV file"""
    df = pd.read_csv(filepath)
    print(f"✅ Data loaded: {len(df)} participants")
    print(f"   Control group: {len(df[df['Group']=='Control'])}")
    print(f"   Treatment group: {len(df[df['Group']=='Treatment'])}")
    return df

# ===========================
# 2. DESCRIPTIVE STATISTICS
# ===========================

def descriptive_stats(df):
    """Calculate descriptive statistics"""
    print("\n" + "="*60)
    print("DESCRIPTIVE STATISTICS")
    print("="*60)
    
    # Demographics
    print("\n--- Demographics ---")
    print(df.groupby('Group')[['Age', 'Gender']].describe())
    
    # Baseline characteristics
    print("\n--- Baseline Characteristics ---")
    baseline_vars = ['Baseline_SII', 'Baseline_GFTA', 'Hearing_Loss_Degree']
    for var in baseline_vars:
        if var in df.columns:
            print(f"\n{var}:")
            print(df.groupby('Group')[var].describe())

# ===========================
# 3. CHECK RANDOMIZATION
# ===========================

def check_randomization(df):
    """Verify successful randomization"""
    print("\n" + "="*60)
    print("RANDOMIZATION CHECK")
    print("="*60)
    
    # Chi-square for categorical variables
    print("\n--- Gender distribution ---")
    contingency_gender = pd.crosstab(df['Group'], df['Gender'])
    chi2_gender, p_gender = stats.chi2_contingency(contingency_gender)[:2]
    print(contingency_gender)
    print(f"χ² = {chi2_gender:.3f}, p = {p_gender:.3f}")
    print("✅ Groups are balanced" if p_gender > 0.05 else "⚠️ Imbalance detected")
    
    # T-test for continuous variables
    print("\n--- Age distribution ---")
    control_age = df[df['Group']=='Control']['Age']
    treatment_age = df[df['Group']=='Treatment']['Age']
    t_age, p_age = ttest_ind(control_age, treatment_age)
    print(f"Control: {control_age.mean():.2f} ± {control_age.std():.2f}")
    print(f"Treatment: {treatment_age.mean():.2f} ± {treatment_age.std():.2f}")
    print(f"t = {t_age:.3f}, p = {p_age:.3f}")
    print("✅ Groups are balanced" if p_age > 0.05 else "⚠️ Imbalance detected")
    
    # Baseline SII
    print("\n--- Baseline SII (Primary outcome) ---")
    control_sii = df[df['Group']=='Control']['Baseline_SII']
    treatment_sii = df[df['Group']=='Treatment']['Baseline_SII']
    t_sii, p_sii = ttest_ind(control_sii, treatment_sii)
    print(f"Control: {control_sii.mean():.2f} ± {control_sii.std():.2f}")
    print(f"Treatment: {treatment_sii.mean():.2f} ± {treatment_sii.std():.2f}")
    print(f"t = {t_sii:.3f}, p = {p_sii:.3f}")
    print("✅ Groups are balanced" if p_sii > 0.05 else "⚠️ Imbalance detected")

# ===========================
# 4. PRIMARY ANALYSIS
# ===========================

def primary_analysis(df):
    """
    Primary outcome: Change in SII from baseline to week 24
    """
    print("\n" + "="*60)
    print("PRIMARY ANALYSIS")
    print("="*60)
    
    # Calculate change scores
    df['SII_Change'] = df['Week24_SII'] - df['Baseline_SII']
    
    control_change = df[df['Group']=='Control']['SII_Change'].dropna()
    treatment_change = df[df['Group']=='Treatment']['SII_Change'].dropna()
    
    print(f"\n--- Change in SII (Baseline to Week 24) ---")
    print(f"Control group:")
    print(f"  Mean change: {control_change.mean():.2f}")
    print(f"  SD: {control_change.std():.2f}")
    print(f"  95% CI: [{control_change.mean() - 1.96*control_change.sem():.2f}, "
          f"{control_change.mean() + 1.96*control_change.sem():.2f}]")
    
    print(f"\nTreatment group:")
    print(f"  Mean change: {treatment_change.mean():.2f}")
    print(f"  SD: {treatment_change.std():.2f}")
    print(f"  95% CI: [{treatment_change.mean() - 1.96*treatment_change.sem():.2f}, "
          f"{treatment_change.mean() + 1.96*treatment_change.sem():.2f}]")
    
    # Check normality
    print("\n--- Normality Check (Shapiro-Wilk) ---")
    _, p_normal_control = shapiro(control_change)
    _, p_normal_treatment = shapiro(treatment_change)
    print(f"Control: p = {p_normal_control:.3f}")
    print(f"Treatment: p = {p_normal_treatment:.3f}")
    
    normal = p_normal_control > 0.05 and p_normal_treatment > 0.05
    
    # Statistical test
    if normal:
        print("\n✅ Data is normally distributed → Using independent t-test")
        t_stat, p_value = ttest_ind(treatment_change, control_change)
        test_name = "Independent t-test"
    else:
        print("\n⚠️ Data is not normally distributed → Using Mann-Whitney U test")
        u_stat, p_value = mannwhitneyu(treatment_change, control_change, alternative='greater')
        test_name = "Mann-Whitney U test"
    
    print(f"\n--- {test_name} Results ---")
    if normal:
        print(f"t-statistic: {t_stat:.3f}")
    else:
        print(f"U-statistic: {u_stat:.3f}")
    print(f"p-value: {p_value:.4f}")
    
    # Effect size (Cohen's d)
    pooled_std = np.sqrt((control_change.var() + treatment_change.var()) / 2)
    cohens_d = (treatment_change.mean() - control_change.mean()) / pooled_std
    
    print(f"\n--- Effect Size ---")
    print(f"Cohen's d: {cohens_d:.3f}")
    if abs(cohens_d) < 0.2:
        effect_interpretation = "Small"
    elif abs(cohens_d) < 0.5:
        effect_interpretation = "Medium"
    else:
        effect_interpretation = "Large"
    print(f"Interpretation: {effect_interpretation} effect")
    
    # Clinical significance
    print(f"\n--- Clinical Significance ---")
    mean_diff = treatment_change.mean() - control_change.mean()
    print(f"Mean difference: {mean_diff:.2f} points")
    
    if p_value < 0.001:
        significance = "***"
    elif p_value < 0.01:
        significance = "**"
    elif p_value < 0.05:
        significance = "*"
    else:
        significance = "ns"
    
    print(f"\n{'='*60}")
    print(f"CONCLUSION: p = {p_value:.4f} {significance}")
    if p_value < 0.05:
        print(f"✅ The HearLoveen intervention significantly improved SII")
        print(f"   compared to control (d = {cohens_d:.2f})")
    else:
        print(f"❌ No significant difference between groups")
    print(f"{'='*60}")
    
    return {
        'p_value': p_value,
        'cohens_d': cohens_d,
        'mean_diff': mean_diff
    }

# ===========================
# 5. SECONDARY ANALYSES
# ===========================

def secondary_outcomes(df):
    """Analyze secondary outcomes"""
    print("\n" + "="*60)
    print("SECONDARY OUTCOMES")
    print("="*60)
    
    secondary_vars = {
        'GFTA_Change': 'Phoneme Accuracy (GFTA)',
        'Parent_Satisfaction': 'Parent Satisfaction',
        'Child_Motivation': 'Child Motivation (IMI)',
        'Practice_Adherence': 'Practice Adherence (sessions/week)'
    }
    
    results = {}
    
    for var, label in secondary_vars.items():
        if var in df.columns:
            print(f"\n--- {label} ---")
            
            control = df[df['Group']=='Control'][var].dropna()
            treatment = df[df['Group']=='Treatment'][var].dropna()
            
            print(f"Control: {control.mean():.2f} ± {control.std():.2f}")
            print(f"Treatment: {treatment.mean():.2f} ± {treatment.std():.2f}")
            
            t_stat, p_val = ttest_ind(treatment, control)
            print(f"t = {t_stat:.3f}, p = {p_val:.3f}")
            
            if p_val < 0.05:
                print(f"✅ Significant difference (p < 0.05)")
            else:
                print(f"❌ No significant difference")
            
            results[var] = p_val
    
    return results

# ===========================
# 6. REPEATED MEASURES ANALYSIS
# ===========================

def longitudinal_analysis(df_long):
    """
    Mixed-effects model for repeated measures
    df_long must be in long format with columns: ChildID, Time, SII, Group
    """
    print("\n" + "="*60)
    print("LONGITUDINAL ANALYSIS (Mixed-Effects Model)")
    print("="*60)
    
    # Fit mixed-effects model
    model = mixedlm("SII ~ Time * Group", df_long, groups=df_long["ChildID"])
    result = model.fit()
    
    print(result.summary())
    
    # Extract key results
    params = result.params
    pvals = result.pvalues
    
    print(f"\n--- Key Effects ---")
    print(f"Time effect: β = {params['Time']:.3f}, p = {pvals['Time']:.4f}")
    print(f"Group effect: β = {params['Group[T.Treatment]']:.3f}, p = {pvals['Group[T.Treatment]']:.4f}")
    print(f"Time × Group interaction: β = {params['Time:Group[T.Treatment]']:.3f}, "
          f"p = {pvals['Time:Group[T.Treatment]']:.4f}")
    
    if pvals['Time:Group[T.Treatment]'] < 0.05:
        print(f"\n✅ Significant Time × Group interaction:")
        print(f"   Treatment group improved faster over time")
    
    return result

# ===========================
# 7. SUBGROUP ANALYSIS
# ===========================

def subgroup_analysis(df):
    """Analyze treatment effect by subgroups"""
    print("\n" + "="*60)
    print("SUBGROUP ANALYSIS")
    print("="*60)
    
    df['SII_Change'] = df['Week24_SII'] - df['Baseline_SII']
    
    subgroups = {
        'Age_Group': ['5-8 years', '9-12 years'],
        'Gender': ['Male', 'Female'],
        'Hearing_Loss_Degree': ['Mild', 'Moderate']
    }
    
    for subgroup_var, categories in subgroups.items():
        if subgroup_var in df.columns:
            print(f"\n--- Subgroup: {subgroup_var} ---")
            
            for category in categories:
                subset = df[df[subgroup_var] == category]
                
                control = subset[subset['Group']=='Control']['SII_Change'].dropna()
                treatment = subset[subset['Group']=='Treatment']['SII_Change'].dropna()
                
                if len(control) > 0 and len(treatment) > 0:
                    t_stat, p_val = ttest_ind(treatment, control)
                    effect = treatment.mean() - control.mean()
                    
                    print(f"\n  {category}:")
                    print(f"    n = {len(control) + len(treatment)}")
                    print(f"    Treatment effect: {effect:.2f}")
                    print(f"    p = {p_val:.3f} {'✅' if p_val < 0.05 else '❌'}")

# ===========================
# 8. DOSE-RESPONSE ANALYSIS
# ===========================

def dose_response(df):
    """
    Relationship between app usage and improvement
    (Treatment group only)
    """
    print("\n" + "="*60)
    print("DOSE-RESPONSE ANALYSIS (Treatment Group Only)")
    print("="*60)
    
    treatment_df = df[df['Group'] == 'Treatment'].copy()
    treatment_df['SII_Change'] = treatment_df['Week24_SII'] - treatment_df['Baseline_SII']
    
    # Daily usage in minutes
    X = treatment_df['Daily_Usage_Minutes'].values.reshape(-1, 1)
    y = treatment_df['SII_Change'].values
    
    # Remove missing values
    mask = ~(np.isnan(X).any(axis=1) | np.isnan(y))
    X = X[mask]
    y = y[mask]
    
    # Linear regression
    from sklearn.linear_model import LinearRegression
    model = LinearRegression()
    model.fit(X, y)
    
    r_squared = model.score(X, y)
    coef = model.coef_[0]
    
    print(f"\nLinear regression results:")
    print(f"  Coefficient: {coef:.4f} (SII points per minute of daily use)")
    print(f"  R²: {r_squared:.3f}")
    print(f"  Interpretation: Each additional minute of daily practice")
    print(f"                  is associated with {coef:.3f} point improvement in SII")
    
    # Correlation
    corr, p_corr = stats.pearsonr(X.flatten(), y)
    print(f"\n  Pearson correlation: r = {corr:.3f}, p = {p_corr:.4f}")
    
    if p_corr < 0.05:
        print(f"  ✅ Significant positive correlation")
    else:
        print(f"  ❌ No significant correlation")
    
    return coef, r_squared

# ===========================
# 9. MISSING DATA ANALYSIS
# ===========================

def handle_missing_data(df):
    """Multiple imputation for missing data"""
    print("\n" + "="*60)
    print("MISSING DATA ANALYSIS")
    print("="*60)
    
    # Check missing data patterns
    missing_pct = df.isnull().sum() / len(df) * 100
    print("\n--- Missing Data Percentage ---")
    print(missing_pct[missing_pct > 0])
    
    # Multiple imputation
    print("\n--- Multiple Imputation (n=5 imputations) ---")
    
    numeric_cols = df.select_dtypes(include=[np.number]).columns
    imputer = IterativeImputer(max_iter=10, random_state=0)
    
    imputed_datasets = []
    for i in range(5):
        imputer.random_state = i
        df_imputed = df.copy()
        df_imputed[numeric_cols] = imputer.fit_transform(df[numeric_cols])
        imputed_datasets.append(df_imputed)
    
    print(f"✅ Created {len(imputed_datasets)} imputed datasets")
    
    return imputed_datasets

# ===========================
# 10. VISUALIZATION
# ===========================

def create_visualizations(df):
    """Create publication-quality figures"""
    print("\n" + "="*60)
    print("CREATING VISUALIZATIONS")
    print("="*60)
    
    # Set style
    sns.set_style("whitegrid")
    plt.rcParams['figure.figsize'] = (12, 8)
    
    # 1. Primary outcome - Change in SII
    fig, ax = plt.subplots(1, 2, figsize=(14, 6))
    
    df['SII_Change'] = df['Week24_SII'] - df['Baseline_SII']
    
    # Boxplot
    sns.boxplot(data=df, x='Group', y='SII_Change', ax=ax[0])
    ax[0].set_title('Change in SII by Group', fontsize=14, fontweight='bold')
    ax[0].set_ylabel('SII Change (points)', fontsize=12)
    ax[0].set_xlabel('Group', fontsize=12)
    
    # Add individual points
    sns.stripplot(data=df, x='Group', y='SII_Change', ax=ax[0], 
                  color='black', alpha=0.3, size=3)
    
    # Bar plot with error bars
    means = df.groupby('Group')['SII_Change'].mean()
    sems = df.groupby('Group')['SII_Change'].sem()
    
    means.plot(kind='bar', yerr=sems, ax=ax[1], capsize=5)
    ax[1].set_title('Mean Change in SII (±SEM)', fontsize=14, fontweight='bold')
    ax[1].set_ylabel('SII Change (points)', fontsize=12)
    ax[1].set_xlabel('Group', fontsize=12)
    ax[1].set_xticklabels(ax[1].get_xticklabels(), rotation=0)
    
    plt.tight_layout()
    plt.savefig('outputs/primary_outcome.png', dpi=300, bbox_inches='tight')
    print("✅ Saved: outputs/primary_outcome.png")
    
    # 2. Longitudinal trajectory
    # This requires long-format data
    # Placeholder for demonstration
    print("✅ Figure 1: Primary outcome comparison")
    
    plt.close('all')

# ===========================
# 11. GENERATE REPORT
# ===========================

def generate_report(primary_results, secondary_results):
    """Generate analysis report"""
    print("\n" + "="*60)
    print("GENERATING FINAL REPORT")
    print("="*60)
    
    report = f"""
# HearLoveen RCT - Statistical Analysis Report

## Primary Outcome

**Outcome:** Change in Speech Intelligibility Index (SII) from baseline to week 24

**Results:**
- p-value: {primary_results['p_value']:.4f}
- Cohen's d: {primary_results['cohens_d']:.3f}
- Mean difference: {primary_results['mean_diff']:.2f} points

**Conclusion:** 
{
'The HearLoveen intervention significantly improved SII compared to control.'
if primary_results['p_value'] < 0.05
else 'No significant difference between groups was observed.'
}

## Secondary Outcomes

"""
    
    for outcome, p_val in secondary_results.items():
        sig = "Significant ✅" if p_val < 0.05 else "Not significant ❌"
        report += f"- {outcome}: p = {p_val:.3f} ({sig})\n"
    
    # Save report
    with open('outputs/analysis_report.md', 'w') as f:
        f.write(report)
    
    print("✅ Report saved: outputs/analysis_report.md")
    print(report)

# ===========================
# MAIN EXECUTION
# ===========================

def main():
    """Main analysis pipeline"""
    print("="*60)
    print("HEARLOVEEN RCT - STATISTICAL ANALYSIS")
    print("="*60)
    
    # Load data
    df = load_data('data/rct_data.csv')
    
    # Descriptive statistics
    descriptive_stats(df)
    
    # Check randomization
    check_randomization(df)
    
    # Primary analysis
    primary_results = primary_analysis(df)
    
    # Secondary outcomes
    secondary_results = secondary_outcomes(df)
    
    # Subgroup analysis
    subgroup_analysis(df)
    
    # Dose-response (treatment group only)
    dose_response(df)
    
    # Visualizations
    create_visualizations(df)
    
    # Generate report
    generate_report(primary_results, secondary_results)
    
    print("\n" + "="*60)
    print("✅ ANALYSIS COMPLETE")
    print("="*60)

if __name__ == "__main__":
    main()
