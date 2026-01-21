import { startApp } from "xmlui";
import pdf from 'xmlui-pdf';
export const runtime = import.meta.glob(`/src/**`, { eager: true });
startApp(runtime, [pdf]);

if (import.meta.hot) {
    import.meta.hot.accept((newModule) => {
        startApp(newModule?.runtime, [pdf]);
    });
}