export type Lang = 'en' | 'ar'

/**
 * UI copy, keyed. Arabic is Modern Standard Arabic in the same calm,
 * non-promotional register the backend uses for LLM output (§ 3.6) — an
 * Egyptian retail investor should read the same honest tone in either language.
 *
 * Groundwork scope: navigation, dashboard, and the Plan page (the goal-based
 * product surface). Remaining pages fall back to English until translated —
 * `t()` returns the English string for any key missing from `ar`, so an
 * untranslated screen degrades to readable rather than blank.
 */
export const STRINGS: Record<Lang, Record<string, string>> = {
    en: {
        // nav / shell
        'nav.dashboard': 'Dashboard',
        'nav.plan': 'Plan',
        'nav.portfolios': 'Portfolios',
        'nav.learning': 'Learning',
        'nav.market': 'Market',
        'nav.profile': 'Profile',
        'nav.logout': 'Log out',
        'nav.account': 'Account',
        'lang.switch': 'العربية',

        // dashboard
        'dash.eyebrow': "Today's readout",
        'dash.title': 'Dashboard',
        'dash.noGoal': 'No goal yet',
        'dash.noGoalCopy': 'Complete the questionnaire to get personalized, risk-graded picks.',
        'dash.startOnboarding': 'Start onboarding',
        'dash.recentActivity': 'Recent Activity',
        'dash.noActivity': 'No activity yet',
        'dash.noActivityHint': 'Notifications will appear here.',
        'dash.loadingGoal': 'Loading your goal…',
        'dash.loadGoalFailed': 'Failed to load your goal.',

        // shared profile labels
        'profile.riskProfile': 'Risk Profile',
        'profile.effectiveRisk': 'Effective Risk',
        'profile.capacity': 'Capacity',
        'profile.tolerance': 'Tolerance',
        'profile.goal': 'Goal',
        'profile.horizon': 'Horizon',

        // goal types
        'goal.Retirement': 'Retirement',
        'goal.LongTermWealth': 'Long-term wealth',
        'goal.MediumTermGoal': 'Medium-term goal',
        'goal.SpeculationLearning': 'Speculation & learning',

        // server enums surfaced in the UI
        'band.Conservative': 'Conservative',
        'band.Moderate': 'Moderate',
        'band.Aggressive': 'Aggressive',
        'engagement.SetAndForget': 'Set & forget',
        'engagement.Monthly': 'Monthly',
        'engagement.Daily': 'Daily',
        'status.Proposed': 'Proposed',
        'status.Accepted': 'Accepted',
        'status.Superseded': 'Superseded',
        'sleeve.core': 'core',
        'sleeve.tactical': 'tactical',
        'sleeve.stability': 'stability',
        'sleeve.speculative': 'speculative',

        // plan page
        'plan.eyebrow': 'Your plan',
        'plan.years': 'year horizon',
        'plan.engagement': 'engagement',
        'plan.speculativeUnlocked': 'Speculative opportunities unlocked — always capped and clearly labeled.',
        'plan.proposal': 'Portfolio proposal',
        'plan.generate': 'Generate proposal',
        'plan.regenerate': 'Regenerate',
        'plan.generating': 'Generating…',
        'plan.noProposal': 'No proposal yet',
        'plan.noProposalHint': 'Generate one to see a deterministic, risk-graded allocation for this goal.',
        'plan.accept': 'Accept this plan',
        'plan.accepting': 'Accepting…',
        'plan.acceptedPlan': 'Accepted plan',
        'plan.superseded': 'Superseded — generate a fresh one',
        'plan.history': 'Proposal history',
        'plan.audit': 'audit',
        'plan.noGoal': 'No goal yet',
        'plan.noGoalCopy': "Answer the questionnaire and we'll build a portfolio proposal tuned to your goal.",
        'plan.startQuestionnaire': 'Start questionnaire',

        // live portfolio
        'live.title': 'Your portfolio',
        'live.live': 'Live · registry closes',
        'live.value': 'Value',
        'live.totalReturn': 'Total Return',
        'live.fromHigh': 'From High',
        'live.atHigh': 'At high',
        'live.nextReview': 'Next Review',
        'live.symbol': 'Symbol',
        'live.target': 'Target',
        'live.actual': 'Actual',
        'live.drift': 'Drift',
        'live.started': 'started',
        'live.rebalances': 'rebalances',

        // track record
        'track.title': 'Our track record',
        'track.hitRate': 'Direction Hit Rate',
        'track.avgReturn': 'Avg Realized Return',
        'track.scored': 'Predictions Scored',
        'track.allTime': 'All Time',
        'track.empty': 'Not enough scored predictions yet',
        'track.emptyHint': 'Predictions are scored once their 30-day horizon matures. This fills in as outcomes land.',
        'track.note': 'Every prediction is scored against what the market actually did 30 days later — wins and losses alike. Past results never guarantee future ones. Informational only, not financial advice.',
        'track.more': 'Full record and methodology',
    },
    ar: {
        // nav / shell
        'nav.dashboard': 'لوحة التحكم',
        'nav.plan': 'الخطة',
        'nav.portfolios': 'المحفظة',
        'nav.learning': 'التعلّم',
        'nav.market': 'السوق',
        'nav.profile': 'الملف الشخصي',
        'nav.logout': 'تسجيل الخروج',
        'nav.account': 'الحساب',
        'lang.switch': 'English',

        // dashboard
        'dash.eyebrow': 'قراءة اليوم',
        'dash.title': 'لوحة التحكم',
        'dash.noGoal': 'لا يوجد هدف بعد',
        'dash.noGoalCopy': 'أكمل الاستبيان للحصول على اختيارات مخصصة ومصنّفة حسب المخاطرة.',
        'dash.startOnboarding': 'ابدأ الإعداد',
        'dash.recentActivity': 'النشاط الأخير',
        'dash.noActivity': 'لا يوجد نشاط بعد',
        'dash.noActivityHint': 'ستظهر الإشعارات هنا.',
        'dash.loadingGoal': 'جارٍ تحميل هدفك…',
        'dash.loadGoalFailed': 'تعذّر تحميل هدفك.',

        // shared profile labels
        'profile.riskProfile': 'مستوى المخاطرة',
        'profile.effectiveRisk': 'المخاطرة الفعلية',
        'profile.capacity': 'القدرة على تحمّل المخاطر',
        'profile.tolerance': 'تقبّل المخاطر',
        'profile.goal': 'الهدف',
        'profile.horizon': 'المدة',

        // goal types
        'goal.Retirement': 'التقاعد',
        'goal.LongTermWealth': 'بناء ثروة طويلة الأجل',
        'goal.MediumTermGoal': 'هدف متوسط الأجل',
        'goal.SpeculationLearning': 'المضاربة والتعلّم',

        // server enums surfaced in the UI
        'band.Conservative': 'متحفّظ',
        'band.Moderate': 'متوازن',
        'band.Aggressive': 'جريء',
        'engagement.SetAndForget': 'دون متابعة',
        'engagement.Monthly': 'شهريًا',
        'engagement.Daily': 'يوميًا',
        'status.Proposed': 'مقترحة',
        'status.Accepted': 'معتمدة',
        'status.Superseded': 'مستبدلة',
        'sleeve.core': 'أساسي',
        'sleeve.tactical': 'تكتيكي',
        'sleeve.stability': 'استقرار',
        'sleeve.speculative': 'مضاربي',

        // plan page
        'plan.eyebrow': 'خطتك',
        'plan.years': 'سنوات',
        'plan.engagement': 'مستوى المتابعة',
        'plan.speculativeUnlocked': 'تم تفعيل الفرص المضاربية — بحدٍّ أقصى دائمًا ومعلَّمة بوضوح.',
        'plan.proposal': 'مقترح المحفظة',
        'plan.generate': 'إنشاء مقترح',
        'plan.regenerate': 'إعادة الإنشاء',
        'plan.generating': 'جارٍ الإنشاء…',
        'plan.noProposal': 'لا يوجد مقترح بعد',
        'plan.noProposalHint': 'أنشئ مقترحًا لعرض توزيع محدَّد ومصنّف حسب المخاطرة لهذا الهدف.',
        'plan.accept': 'اعتماد هذه الخطة',
        'plan.accepting': 'جارٍ الاعتماد…',
        'plan.acceptedPlan': 'الخطة المعتمدة',
        'plan.superseded': 'تم استبدالها — أنشئ مقترحًا جديدًا',
        'plan.history': 'سجل المقترحات',
        'plan.audit': 'مرجع التدقيق',
        'plan.noGoal': 'لا يوجد هدف بعد',
        'plan.noGoalCopy': 'أجب عن الاستبيان وسنبني لك مقترح محفظة مناسبًا لهدفك.',
        'plan.startQuestionnaire': 'ابدأ الاستبيان',

        // live portfolio
        'live.title': 'محفظتك',
        'live.live': 'مباشر · أسعار الإغلاق',
        'live.value': 'القيمة',
        'live.totalReturn': 'إجمالي العائد',
        'live.fromHigh': 'من أعلى مستوى',
        'live.atHigh': 'عند أعلى مستوى',
        'live.nextReview': 'المراجعة القادمة',
        'live.symbol': 'الرمز',
        'live.target': 'المستهدف',
        'live.actual': 'الفعلي',
        'live.drift': 'الانحراف',
        'live.started': 'بدأت في',
        'live.rebalances': 'إعادة التوازن',

        // track record
        'track.title': 'سجل أدائنا',
        'track.hitRate': 'نسبة إصابة الاتجاه',
        'track.avgReturn': 'متوسط العائد المحقق',
        'track.scored': 'التوقعات المُقيّمة',
        'track.allTime': 'منذ البداية',
        'track.empty': 'لا توجد توقعات مُقيّمة كافية بعد',
        'track.emptyHint': 'تُقيَّم التوقعات بعد اكتمال مدة الثلاثين يومًا. ستمتلئ هذه الأرقام تباعًا.',
        'track.note': 'كل توقع يُقيَّم مقابل ما فعله السوق فعليًا بعد 30 يومًا — المكاسب والخسائر على حد سواء. النتائج السابقة لا تضمن النتائج المستقبلية. لأغراض معلوماتية فقط وليست نصيحة مالية.',
        'track.more': 'السجل الكامل والمنهجية',
    },
}
