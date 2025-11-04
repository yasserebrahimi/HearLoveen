
# Outcome Toolkit: effect size & power (placeholders)
import math
def cohens_d(mean1, mean2, sd1, sd2, n1, n2):
    sp = math.sqrt(((n1-1)*sd1**2 + (n2-1)*sd2**2) / (n1+n2-2))
    return (mean1 - mean2)/sp
def power_t_test(d, alpha=0.05, n=50):
    # very rough approximation
    return min(0.99, max(0.0, 0.5 + d*math.sqrt(n)/5.0))
