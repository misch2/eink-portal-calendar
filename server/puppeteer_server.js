const puppeteer = require("puppeteer");
const express = require("express");

(async () => {
    const app = express();
    const port = process.env.PORT || 9876;
    var browser;

    // create a browser instance
    app.listen(port, "localhost", () => {
        console.log(`Example app listening on port ${port}!`);
        console.log(
            `Run http://localhost:${port}/screenshot?w=640&h=480&url=https://example.org/ in your browser.`
        );
    });

    app.get("/", (req, res) => res.send("Hello World!"));
    app.get("/screenshot", async (req, res) => {
        try {
            if (!browser) {
                console.log("launching browser");
                browser = await puppeteer.launch({
                    headless: "new",
                    executablePath: "/usr/bin/chromium-browser",
                });
                console.log("browser launched");
            } else {
                console.log("reusing browser");
            }

            const page = await browser.newPage();
            await page.setViewport({
                width: parseInt(req.query.w),
                height: parseInt(req.query.h),
            });
            console.log(
                `Generating screenshot for ${req.query.url} with ${req.query.w}x${req.query.h}`
            );
            await page.goto(req.query.url);

            const imageBuffer = await page.screenshot();

            console.log(
                `Screenshot generated, image size is ${imageBuffer.length} bytes.`
            );

            res.set("Content-Type", "image/png");
            res.set("Content-Length", imageBuffer.length);
            res.send(imageBuffer);
        } catch (e) {
            console.log(`Exception when fetching ${req.query.url}: ${e}`);
            await browser.close();
            browser = null;
            res.status(500).send(e.toString());
        }
    });
})();
