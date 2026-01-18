const fs = require('fs');
const path = require('path');

const resultsPath = path.join(__dirname, 'test-results.json');
const outputDir = path.join(__dirname, '..', 'documentation');
const outputPath = path.join(outputDir, 'e2e_test_report.md');
const screenshotsDir = path.join(outputDir, 'screenshots');

if (!fs.existsSync(resultsPath)) {
    console.error('Test results file not found:', resultsPath);
    process.exit(1);
}

const data = JSON.parse(fs.readFileSync(resultsPath, 'utf8'));

let md = '# E2E Test Report\n\n';
md += `**Date:** ${new Date().toLocaleString()}\n`;
md += `**Total Tests:** ${data.stats.expected}\n`;
md += `**Passed:** ${data.stats.expected - data.stats.unexpected}\n`;
md += `**Failed:** ${data.stats.unexpected}\n\n`;

md += '## Details\n\n';

data.suites.forEach(suite => {
    md += `### ${suite.title}\n`;
    suite.specs.forEach(spec => {
        md += `#### ${spec.title}\n`;
        spec.tests.forEach(test => {
            const statusIcon = test.status === 'expected' ? '✅' : '❌';
            md += `- Status: ${statusIcon} ${test.status}\n`;
            md += `- Duration: ${test.results[0].duration}ms\n`;

            // Check for screenshots
            // Screenshots are saved manually in tests or via config.
            // If config 'screenshot: on', they are in attachments.
            // But we also manually saved them using BasePage.
            // We can match them by name if we followed naming convention.
        });
        md += '\n';
    });
});

md += '## Screenshots\n\n';
md += 'Screenshots are saved in the `screenshots` folder.\n';

if (fs.existsSync(screenshotsDir)) {
    const files = fs.readdirSync(screenshotsDir);
    files.forEach(file => {
        md += `![${file}](./screenshots/${file})\n\n`;
    });
}

fs.writeFileSync(outputPath, md);
console.log(`Report generated at: ${outputPath}`);
