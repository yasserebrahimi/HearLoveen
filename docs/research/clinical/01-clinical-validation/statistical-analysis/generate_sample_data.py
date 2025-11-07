"""
Generate Sample RCT Data for HearLoveen Analysis
================================================

This script generates synthetic data for testing the RCT analysis pipeline.
"""

import numpy as np
import pandas as pd
from datetime import datetime, timedelta

np.random.seed(42)

# Sample size
n_treatment = 100
n_control = 100
n_total = n_treatment + n_control

print("🔧 Generating sample RCT data...")

# Generate baseline characteristics
data = {
    'ChildID': [f'CH{i:03d}' for i in range(1, n_total + 1)],
    'Group': ['Treatment'] * n_treatment + ['Control'] * n_control,
    'Age': np.concatenate([
        np.random.randint(5, 13, n_treatment),
        np.random.randint(5, 13, n_control)
    ]),
    'Gender': np.random.choice(['Male', 'Female'], n_total),
    'Hearing_Loss_Degree': np.random.choice(['Mild', 'Moderate'], n_total),
    'Baseline_SII': np.random.normal(60, 10, n_total).clip(20, 100),
    'Baseline_GFTA': np.random.normal(70, 12, n_total).clip(30, 100),
}

df = pd.DataFrame(data)

# Generate treatment effect
# Control group: small improvement (5 ± 5 points)
# Treatment group: larger improvement (15 ± 6 points)

control_mask = df['Group'] == 'Control'
treatment_mask = df['Group'] == 'Treatment'

# Week 24 outcomes
df['Week24_SII'] = df['Baseline_SII'].copy()
df.loc[control_mask, 'Week24_SII'] += np.random.normal(5, 5, control_mask.sum())
df.loc[treatment_mask, 'Week24_SII'] += np.random.normal(15, 6, treatment_mask.sum())
df['Week24_SII'] = df['Week24_SII'].clip(20, 100)

df['Week24_GFTA'] = df['Baseline_GFTA'].copy()
df.loc[control_mask, 'Week24_GFTA'] += np.random.normal(8, 5, control_mask.sum())
df.loc[treatment_mask, 'Week24_GFTA'] += np.random.normal(18, 7, treatment_mask.sum())
df['Week24_GFTA'] = df['Week24_GFTA'].clip(30, 100)

# Calculate changes
df['SII_Change'] = df['Week24_SII'] - df['Baseline_SII']
df['GFTA_Change'] = df['Week24_GFTA'] - df['Baseline_GFTA']

# Parent satisfaction (1-5 scale)
df['Parent_Satisfaction'] = np.random.normal(4.0, 0.5, n_total).clip(1, 5)
df.loc[treatment_mask, 'Parent_Satisfaction'] += np.random.normal(0.3, 0.2, treatment_mask.sum())
df['Parent_Satisfaction'] = df['Parent_Satisfaction'].clip(1, 5)

# Practice adherence (sessions per week)
df['Practice_Adherence'] = np.random.poisson(4, n_total)
df.loc[treatment_mask, 'Practice_Adherence'] = np.random.poisson(12, treatment_mask.sum())

# Daily usage (minutes/day) - only for treatment group
df['Daily_Usage_Minutes'] = 0
df.loc[treatment_mask, 'Daily_Usage_Minutes'] = np.random.normal(18, 5, treatment_mask.sum()).clip(5, 40)

# Child motivation (1-5 scale)
df['Child_Motivation'] = np.random.normal(3.5, 0.7, n_total).clip(1, 5)
df.loc[treatment_mask, 'Child_Motivation'] += np.random.normal(0.5, 0.3, treatment_mask.sum())
df['Child_Motivation'] = df['Child_Motivation'].clip(1, 5)

# Add some realistic patterns
# Better baseline → harder to improve (ceiling effect)
ceiling_effect = -0.2 * (df['Baseline_SII'] - 60)
df['SII_Change'] += ceiling_effect

# Age effect (older children improve slightly faster)
age_effect = 0.3 * (df['Age'] - 8)
df['SII_Change'] += age_effect

# Clip final values
df['Week24_SII'] = (df['Baseline_SII'] + df['SII_Change']).clip(20, 100)
df['Week24_GFTA'] = (df['Baseline_GFTA'] + df['GFTA_Change']).clip(30, 100)

# Add missing data (realistic dropout)
np.random.seed(43)
missing_week24 = np.random.choice(df.index, size=int(0.15 * n_total), replace=False)
df.loc[missing_week24, 'Week24_SII'] = np.nan
df.loc[missing_week24, 'Week24_GFTA'] = np.nan

# Round numeric columns
numeric_cols = ['Baseline_SII', 'Week24_SII', 'Baseline_GFTA', 'Week24_GFTA',
                'SII_Change', 'GFTA_Change', 'Parent_Satisfaction', 
                'Daily_Usage_Minutes', 'Child_Motivation']
df[numeric_cols] = df[numeric_cols].round(2)

# Save to CSV
output_path = 'rct_data.csv'
df.to_csv(output_path, index=False)

print(f"✅ Sample data generated: {output_path}")
print(f"   Total participants: {len(df)}")
print(f"   Control group: {control_mask.sum()}")
print(f"   Treatment group: {treatment_mask.sum()}")
print(f"   Missing data: {df['Week24_SII'].isna().sum()} ({df['Week24_SII'].isna().sum()/len(df)*100:.1f}%)")

# Display summary statistics
print("\n📊 Summary Statistics:")
print("\nBaseline SII:")
print(df.groupby('Group')['Baseline_SII'].describe()[['mean', 'std']])
print("\nChange in SII (Baseline to Week 24):")
print(df.groupby('Group')['SII_Change'].describe()[['mean', 'std']])

# Quick t-test
from scipy import stats
control_change = df[df['Group']=='Control']['SII_Change'].dropna()
treatment_change = df[df['Group']=='Treatment']['SII_Change'].dropna()
t_stat, p_value = stats.ttest_ind(treatment_change, control_change)

print(f"\n🧪 Quick t-test:")
print(f"   Control improvement: {control_change.mean():.2f} ± {control_change.std():.2f}")
print(f"   Treatment improvement: {treatment_change.mean():.2f} ± {treatment_change.std():.2f}")
print(f"   Difference: {treatment_change.mean() - control_change.mean():.2f}")
print(f"   t = {t_stat:.3f}, p = {p_value:.4f}")

if p_value < 0.05:
    print("   ✅ Significant difference!")
else:
    print("   ❌ No significant difference")

print("\n✅ Ready to use with rct_analysis.py!")
