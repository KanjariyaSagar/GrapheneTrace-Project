
    /* ----------- AUTO-GENERATED USERS FOR TOTAL KPI ---------- */
    // 225 total users:
    // 4 Admins, 41 Clinicians, 180 Patients
    const TOTAL_USERS = 225;

    const firstNames = [
        'Aarav','Vihaan','Isha','Rohan','Neha',
        'Kavya','Arjun','Meera','Dhruv','Ananya',
        'Aditya','Riya','Karan','Sonal','Yash'
    ];

    const lastNames = [
        'Sharma','Patel','Iyer','Rao','Verma',
        'Menon','Khan','Joshi','Desai','Nair',
        'Singh','Kapoor','Bose','Gupta','Reddy'
    ];

    const totalRows = [];
    const clinicianRows = [];
    const patientRows = [];

    for (let i = 1; i <= TOTAL_USERS; i++) {
        const idx = i - 1;
        const f = firstNames[idx % firstNames.length];
        const l = lastNames[Math.floor(idx / firstNames.length) % lastNames.length];
        const fullName = `${f} ${l}`;

        let role;
        let domain;

        if (i <= 4) {
            role = 'Admin';
            domain = 'admin.graphenetrace.com';
        } else if (i <= 4 + 41) {
            role = 'Clinician';
            domain = 'clinician.graphenetrace.com';
        } else {
            role = 'Patient';
            domain = 'patient.graphenetrace.com';
        }

        const email = `${f.toLowerCase()}.${l.toLowerCase()}@${domain}`;
        const id = 'U' + i.toString().padStart(3,'0');

        totalRows.push([id, fullName, role, email]);

        if (role === 'Clinician') {
            clinicianRows.push([id, fullName, email]);
        } else if (role === 'Patient') {
            patientRows.push([id, fullName, email]);
        }
    }


