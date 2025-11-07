"""
HearLoveen - Unit Economics Calculator
======================================

این اسکریپت محاسبات دقیق Unit Economics برای پروژه HearLoveen را انجام می‌دهد.

Author: Yasser Ebrahimi Fard
Date: November 2025
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns

# ===========================
# ASSUMPTIONS
# ===========================

ASSUMPTIONS = {
    # Pricing
    'price_per_patient_year': 100,  # $100/patient/year (clinic pays us)
    'clinic_charges_parents': 175,  # Clinic charges parents $150-200
    
    # Costs
    'cloud_cost_per_patient_month': 2,  # Azure costs
    'support_cost_per_patient_year': 10,  # Customer success
    'payment_processing_fee': 0.029,  # 2.9% Stripe fee
    
    # Sales & Marketing
    'sales_rep_salary': 80000,  # Per year
    'sales_rep_quota_clinics': 50,  # Clinics/year per rep
    'avg_patients_per_clinic': 20,
    'marketing_budget_monthly': 5000,
    
    # Retention
    'churn_rate_annual': 0.15,  # 15% annual churn
    'avg_customer_lifetime_years': 3,
    
    # Growth
    'year1_clinics': 50,
    'year2_clinics': 200,
    'year3_clinics': 500,
    'year4_clinics': 1000,
    'year5_clinics': 2000,
}

# ===========================
# UNIT ECONOMICS
# ===========================

def calculate_unit_economics():
    """محاسبه اقتصاد واحد برای هر بیمار"""
    
    print("="*60)
    print("HEARLOVEEN - UNIT ECONOMICS")
    print("="*60)
    
    # Revenue per patient
    annual_revenue_per_patient = ASSUMPTIONS['price_per_patient_year']
    
    # Variable costs per patient
    cloud_cost_annual = ASSUMPTIONS['cloud_cost_per_patient_month'] * 12
    support_cost_annual = ASSUMPTIONS['support_cost_per_patient_year']
    payment_fee = annual_revenue_per_patient * ASSUMPTIONS['payment_processing_fee']
    
    total_variable_cost = cloud_cost_annual + support_cost_annual + payment_fee
    
    # Gross margin
    gross_profit = annual_revenue_per_patient - total_variable_cost
    gross_margin_pct = (gross_profit / annual_revenue_per_patient) * 100
    
    # Customer Lifetime Value (LTV)
    avg_lifetime = ASSUMPTIONS['avg_customer_lifetime_years']
    ltv = gross_profit * avg_lifetime
    
    print(f"\n--- PER PATIENT (ANNUAL) ---")
    print(f"Revenue:                ${annual_revenue_per_patient:.2f}")
    print(f"  Cloud costs:          -${cloud_cost_annual:.2f}")
    print(f"  Support:              -${support_cost_annual:.2f}")
    print(f"  Payment processing:   -${payment_fee:.2f}")
    print(f"  Total variable cost:  -${total_variable_cost:.2f}")
    print(f"Gross Profit:           ${gross_profit:.2f}")
    print(f"Gross Margin:           {gross_margin_pct:.1f}%")
    print(f"\nLifetime Value (LTV):   ${ltv:.2f}")
    print(f"  (Assuming {avg_lifetime} year avg lifetime)")
    
    return {
        'revenue': annual_revenue_per_patient,
        'cogs': total_variable_cost,
        'gross_profit': gross_profit,
        'gross_margin_pct': gross_margin_pct,
        'ltv': ltv
    }

# ===========================
# CAC CALCULATION
# ===========================

def calculate_cac():
    """محاسبه Customer Acquisition Cost"""
    
    print(f"\n{'='*60}")
    print("CUSTOMER ACQUISITION COST (CAC)")
    print("="*60)
    
    # Sales costs
    sales_rep_salary = ASSUMPTIONS['sales_rep_salary']
    clinics_per_rep = ASSUMPTIONS['sales_rep_quota_clinics']
    patients_per_clinic = ASSUMPTIONS['avg_patients_per_clinic']
    
    # Marketing costs
    marketing_annual = ASSUMPTIONS['marketing_budget_monthly'] * 12
    
    # Year 1 example
    year1_clinics = ASSUMPTIONS['year1_clinics']
    year1_patients = year1_clinics * patients_per_clinic
    
    # Number of sales reps needed
    reps_needed = np.ceil(year1_clinics / clinics_per_rep)
    total_sales_cost = reps_needed * sales_rep_salary
    
    # Total S&M
    total_sales_marketing = total_sales_cost + marketing_annual
    
    # CAC per clinic
    cac_per_clinic = total_sales_marketing / year1_clinics
    
    # CAC per patient
    cac_per_patient = total_sales_marketing / year1_patients
    
    print(f"\n--- YEAR 1 ACQUISITION COSTS ---")
    print(f"Sales reps needed:      {int(reps_needed)}")
    print(f"  @ ${sales_rep_salary:,}/year each")
    print(f"Total sales cost:       ${total_sales_cost:,}")
    print(f"Marketing budget:       ${marketing_annual:,}")
    print(f"Total S&M:              ${total_sales_marketing:,}")
    print(f"\nClinic acquired:        {year1_clinics}")
    print(f"Patients acquired:      {year1_patients}")
    print(f"\nCAC per clinic:         ${cac_per_clinic:.2f}")
    print(f"CAC per patient:        ${cac_per_patient:.2f}")
    
    return {
        'cac_per_clinic': cac_per_clinic,
        'cac_per_patient': cac_per_patient,
        'total_sales_marketing': total_sales_marketing
    }

# ===========================
# LTV:CAC RATIO
# ===========================

def calculate_ltv_cac_ratio(unit_econ, cac):
    """محاسبه نسبت LTV:CAC"""
    
    print(f"\n{'='*60}")
    print("LTV:CAC RATIO")
    print("="*60)
    
    ltv = unit_econ['ltv']
    cac = cac['cac_per_patient']
    
    ltv_cac_ratio = ltv / cac if cac > 0 else 0
    
    print(f"\nLTV:                    ${ltv:.2f}")
    print(f"CAC:                    ${cac:.2f}")
    print(f"LTV:CAC Ratio:          {ltv_cac_ratio:.2f}:1")
    
    print(f"\n--- INTERPRETATION ---")
    if ltv_cac_ratio > 3:
        print(f"✅ Excellent! (>{3}:1 is healthy)")
    elif ltv_cac_ratio > 2:
        print(f"✅ Good (>2:1 is acceptable)")
    elif ltv_cac_ratio > 1:
        print(f"⚠️ Marginal (need to improve)")
    else:
        print(f"❌ Unprofitable (CAC > LTV)")
    
    return ltv_cac_ratio

# ===========================
# PAYBACK PERIOD
# ===========================

def calculate_payback_period(unit_econ, cac):
    """محاسبه Payback Period (CAC Payback)"""
    
    print(f"\n{'='*60}")
    print("CAC PAYBACK PERIOD")
    print("="*60)
    
    monthly_gross_profit = unit_econ['gross_profit'] / 12
    cac_value = cac['cac_per_patient']
    
    payback_months = cac_value / monthly_gross_profit if monthly_gross_profit > 0 else float('inf')
    
    print(f"\nCAC:                    ${cac_value:.2f}")
    print(f"Monthly Gross Profit:   ${monthly_gross_profit:.2f}")
    print(f"Payback Period:         {payback_months:.1f} months")
    
    print(f"\n--- INTERPRETATION ---")
    if payback_months < 12:
        print(f"✅ Excellent! (<12 months is ideal)")
    elif payback_months < 18:
        print(f"✅ Good (<18 months is acceptable)")
    else:
        print(f"⚠️ Long payback period")
    
    return payback_months

# ===========================
# 5-YEAR PROJECTIONS
# ===========================

def five_year_projections(unit_econ, cac):
    """پیش‌بینی 5 ساله"""
    
    print(f"\n{'='*60}")
    print("5-YEAR FINANCIAL PROJECTIONS")
    print("="*60)
    
    years = [1, 2, 3, 4, 5]
    clinics = [
        ASSUMPTIONS['year1_clinics'],
        ASSUMPTIONS['year2_clinics'],
        ASSUMPTIONS['year3_clinics'],
        ASSUMPTIONS['year4_clinics'],
        ASSUMPTIONS['year5_clinics']
    ]
    
    patients_per_clinic = ASSUMPTIONS['avg_patients_per_clinic']
    revenue_per_patient = unit_econ['revenue']
    gross_profit_per_patient = unit_econ['gross_profit']
    
    data = []
    
    for year, clinic_count in zip(years, clinics):
        total_patients = clinic_count * patients_per_clinic
        
        # Revenue
        total_revenue = total_patients * revenue_per_patient
        
        # Gross profit
        total_gross_profit = total_patients * gross_profit_per_patient
        
        # Operating expenses (simplified)
        # Fixed costs scale with team size
        base_opex = 1_000_000  # Year 1 base
        opex = base_opex * (1 + 0.3 * (year - 1))  # 30% increase per year
        
        # EBITDA
        ebitda = total_gross_profit - opex
        ebitda_margin = (ebitda / total_revenue * 100) if total_revenue > 0 else 0
        
        data.append({
            'Year': year,
            'Clinics': clinic_count,
            'Patients': total_patients,
            'Revenue': total_revenue,
            'Gross Profit': total_gross_profit,
            'OpEx': opex,
            'EBITDA': ebitda,
            'EBITDA Margin %': ebitda_margin
        })
    
    df = pd.DataFrame(data)
    
    print(f"\n{df.to_string(index=False)}")
    
    # Break-even analysis
    breakeven_year = df[df['EBITDA'] > 0]['Year'].min()
    if not pd.isna(breakeven_year):
        print(f"\n✅ Break-even achieved in Year {int(breakeven_year)}")
    else:
        print(f"\n⚠️ No break-even within 5 years")
    
    return df

# ===========================
# SENSITIVITY ANALYSIS
# ===========================

def sensitivity_analysis():
    """تحلیل حساسیت"""
    
    print(f"\n{'='*60}")
    print("SENSITIVITY ANALYSIS")
    print("="*60)
    
    # Base case
    base_price = ASSUMPTIONS['price_per_patient_year']
    base_ltv = calculate_unit_economics()['ltv']
    base_cac = calculate_cac()['cac_per_patient']
    base_ratio = base_ltv / base_cac
    
    print(f"\nBase Case:")
    print(f"  Price: ${base_price}, LTV:CAC = {base_ratio:.2f}:1")
    
    # Scenarios
    print(f"\n--- PRICE SENSITIVITY ---")
    prices = [75, 100, 125, 150]
    for price in prices:
        # Recalculate with new price
        revenue = price
        cogs = 24 + 10 + (price * 0.029)
        gross_profit = revenue - cogs
        ltv = gross_profit * 3
        ratio = ltv / base_cac
        
        print(f"  ${price}/patient/year → LTV:CAC = {ratio:.2f}:1 {'✅' if ratio > 3 else '⚠️'}")
    
    print(f"\n--- LIFETIME SENSITIVITY ---")
    lifetimes = [2, 3, 4, 5]
    for lifetime in lifetimes:
        ltv = (base_price - 37.9) * lifetime
        ratio = ltv / base_cac
        
        print(f"  {lifetime} years → LTV:CAC = {ratio:.2f}:1 {'✅' if ratio > 3 else '⚠️'}")
    
    print(f"\n--- CHURN SENSITIVITY ---")
    churn_rates = [0.10, 0.15, 0.20, 0.25]
    for churn in churn_rates:
        lifetime = 1 / churn
        ltv = (base_price - 37.9) * lifetime
        ratio = ltv / base_cac
        
        print(f"  {churn*100:.0f}% churn (lifetime={lifetime:.1f}y) → LTV:CAC = {ratio:.2f}:1 {'✅' if ratio > 3 else '⚠️'}")

# ===========================
# VISUALIZATION
# ===========================

def create_visualizations(df_projections):
    """ایجاد نمودارها"""
    
    print(f"\n{'='*60}")
    print("CREATING VISUALIZATIONS")
    print("="*60)
    
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    
    # 1. Revenue & EBITDA over time
    ax1 = axes[0, 0]
    ax1.plot(df_projections['Year'], df_projections['Revenue'] / 1e6, 
             marker='o', linewidth=2, label='Revenue')
    ax1.plot(df_projections['Year'], df_projections['EBITDA'] / 1e6, 
             marker='s', linewidth=2, label='EBITDA')
    ax1.axhline(y=0, color='r', linestyle='--', alpha=0.5)
    ax1.set_xlabel('Year', fontsize=12)
    ax1.set_ylabel('$ Millions', fontsize=12)
    ax1.set_title('Revenue & EBITDA Projection', fontsize=14, fontweight='bold')
    ax1.legend()
    ax1.grid(True, alpha=0.3)
    
    # 2. EBITDA Margin
    ax2 = axes[0, 1]
    colors = ['red' if x < 0 else 'green' for x in df_projections['EBITDA Margin %']]
    ax2.bar(df_projections['Year'], df_projections['EBITDA Margin %'], color=colors, alpha=0.7)
    ax2.axhline(y=0, color='black', linestyle='-', linewidth=1)
    ax2.set_xlabel('Year', fontsize=12)
    ax2.set_ylabel('EBITDA Margin %', fontsize=12)
    ax2.set_title('EBITDA Margin Progression', fontsize=14, fontweight='bold')
    ax2.grid(True, alpha=0.3)
    
    # 3. Growth metrics
    ax3 = axes[1, 0]
    ax3_twin = ax3.twinx()
    
    ax3.bar(df_projections['Year'], df_projections['Clinics'], alpha=0.6, label='Clinics')
    ax3_twin.plot(df_projections['Year'], df_projections['Patients'], 
                  color='red', marker='o', linewidth=2, label='Patients')
    
    ax3.set_xlabel('Year', fontsize=12)
    ax3.set_ylabel('Clinics', fontsize=12, color='blue')
    ax3_twin.set_ylabel('Patients', fontsize=12, color='red')
    ax3.set_title('Growth: Clinics & Patients', fontsize=14, fontweight='bold')
    ax3.legend(loc='upper left')
    ax3_twin.legend(loc='upper right')
    ax3.grid(True, alpha=0.3)
    
    # 4. Unit Economics
    ax4 = axes[1, 1]
    metrics = ['Revenue\nper Patient', 'Gross Profit\nper Patient', 'LTV', 'CAC']
    values = [100, 62, 186, 60]  # Example values
    colors_metrics = ['#2ecc71', '#3498db', '#9b59b6', '#e74c3c']
    
    bars = ax4.bar(metrics, values, color=colors_metrics, alpha=0.7)
    ax4.set_ylabel('$ Amount', fontsize=12)
    ax4.set_title('Unit Economics Summary', fontsize=14, fontweight='bold')
    ax4.grid(True, alpha=0.3, axis='y')
    
    # Add value labels on bars
    for bar in bars:
        height = bar.get_height()
        ax4.text(bar.get_x() + bar.get_width()/2., height,
                f'${height:.0f}',
                ha='center', va='bottom', fontsize=10)
    
    plt.tight_layout()
    plt.savefig('/home/claude/hearloveen-complete/02-business-model/financial-models/unit_economics_dashboard.png', 
                dpi=300, bbox_inches='tight')
    
    print(f"✅ Saved: unit_economics_dashboard.png")
    plt.close()

# ===========================
# MAIN EXECUTION
# ===========================

def main():
    """اجرای اصلی"""
    
    print("\n" + "🚀 HEARLOVEEN UNIT ECONOMICS CALCULATOR ".center(60, "="))
    print(f"Date: November 7, 2025\n")
    
    # 1. Unit Economics
    unit_econ = calculate_unit_economics()
    
    # 2. CAC
    cac = calculate_cac()
    
    # 3. LTV:CAC Ratio
    ltv_cac = calculate_ltv_cac_ratio(unit_econ, cac)
    
    # 4. Payback Period
    payback = calculate_payback_period(unit_econ, cac)
    
    # 5. 5-Year Projections
    df_proj = five_year_projections(unit_econ, cac)
    
    # 6. Sensitivity Analysis
    sensitivity_analysis()
    
    # 7. Visualizations
    create_visualizations(df_proj)
    
    # Summary
    print(f"\n{'='*60}")
    print("KEY METRICS SUMMARY")
    print("="*60)
    print(f"\n💰 Unit Economics:")
    print(f"   Annual Revenue per Patient: ${unit_econ['revenue']:.2f}")
    print(f"   Gross Margin: {unit_econ['gross_margin_pct']:.1f}%")
    print(f"   LTV: ${unit_econ['ltv']:.2f}")
    
    print(f"\n📈 Growth Metrics:")
    print(f"   CAC per Patient: ${cac['cac_per_patient']:.2f}")
    print(f"   LTV:CAC Ratio: {ltv_cac:.2f}:1 {'✅' if ltv_cac > 3 else '⚠️'}")
    print(f"   Payback Period: {payback:.1f} months")
    
    print(f"\n🎯 5-Year Outlook:")
    print(f"   Year 5 Revenue: ${df_proj.iloc[-1]['Revenue']:,.0f}")
    print(f"   Year 5 EBITDA: ${df_proj.iloc[-1]['EBITDA']:,.0f}")
    print(f"   Year 5 Margin: {df_proj.iloc[-1]['EBITDA Margin %']:.1f}%")
    
    print(f"\n{'='*60}")
    print("✅ ANALYSIS COMPLETE")
    print("="*60 + "\n")

if __name__ == "__main__":
    main()
